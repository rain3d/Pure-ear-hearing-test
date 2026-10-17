using HearingTest.Domain.Interfaces;
using HearingTest.Domain.Models;

namespace HearingTest.Domain.StateMachine;

public class AudiometryStateMachine
{
    private readonly IThresholdProtocol _protocol;
    private readonly Random _random;

    public TestSession CurrentSession { get; private set; }
    public TestProgressState State { get; private set; } = TestProgressState.Idle;

    public Ear CurrentEar { get; private set; } = Ear.Left;
    public bool TestBothEars { get; private set; } = true;
    public Ear FirstTestedEar { get; private set; } = Ear.Left;

    public int CurrentFrequencyIndex { get; private set; } = 0;
    public int CurrentFrequencyHz => _protocol.FrequencySequence.Count > 0
        ? _protocol.FrequencySequence[Math.Clamp(CurrentFrequencyIndex, 0, _protocol.FrequencySequence.Count - 1)]
        : 1000;
    public double CurrentLevelHl { get; private set; } = 30.0; // Standard starting presentation
    public bool IsCurrentStimulusCatchTrial { get; private set; } = false;

    public int TotalCatchTrials { get; private set; } = 0;
    public int FalsePositiveCount { get; private set; } = 0;
    public bool RetestWarningTriggered { get; private set; } = false;
    public double? Initial1000HzThreshold { get; private set; }
    public double? Retest1000HzThreshold { get; private set; }

    public List<AuditLog> AuditLogs { get; } = [];

    public event EventHandler<StimulusEvent>? StimulusPrepared;
    public event EventHandler<ThresholdResult>? ThresholdDetermined;
    public event EventHandler<TestProgressState>? StateChanged;

    public AudiometryStateMachine(IThresholdProtocol? protocol = null, int? randomSeed = null)
    {
        _protocol = protocol ?? new StandardThresholdProtocol();
        _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        CurrentSession = new TestSession();
    }

    public void StartSession(Patient patient, Ear startEar = Ear.Left, bool bothEars = true, double startingLevel = 30.0)
    {
        CurrentSession = new TestSession
        {
            PatientId = patient.Id,
            StartedAt = DateTime.UtcNow,
            ProtocolVersion = _protocol.Version
        };

        CurrentEar = startEar;
        FirstTestedEar = startEar;
        TestBothEars = bothEars;
        CurrentFrequencyIndex = 0;
        CurrentLevelHl = _protocol.ClampLevel(startingLevel);
        TotalCatchTrials = 0;
        FalsePositiveCount = 0;
        RetestWarningTriggered = false;
        Initial1000HzThreshold = null;
        Retest1000HzThreshold = null;

        SetState(TestProgressState.Testing);
    }

    public void Pause()
    {
        if (State == TestProgressState.Testing)
        {
            SetState(TestProgressState.Paused);
        }
    }

    public void Resume()
    {
        if (State == TestProgressState.Paused)
        {
            SetState(TestProgressState.Testing);
        }
    }

    public void Abort(string reason = "Operator aborted")
    {
        SetState(TestProgressState.Aborted);
        CurrentSession.CompletedAt = DateTime.UtcNow;
        CurrentSession.Notes += $" [Aborted: {reason}]";
    }

    public StimulusEvent NextStimulus()
    {
        if (State != TestProgressState.Testing)
        {
            throw new InvalidOperationException($"Cannot generate stimulus in state {State}");
        }

        // Random chance of Catch Trial (12% probability, not immediately after another catch trial)
        bool shouldCatchTrial = !IsCurrentStimulusCatchTrial && _random.NextDouble() < 0.12;
        IsCurrentStimulusCatchTrial = shouldCatchTrial;

        if (shouldCatchTrial)
        {
            TotalCatchTrials++;
        }

        var stimulus = new StimulusEvent
        {
            SessionId = CurrentSession.Id,
            SequenceNo = CurrentSession.StimulusEvents.Count + 1,
            Ear = CurrentEar,
            FrequencyHz = CurrentFrequencyHz,
            ToneLevelHl = shouldCatchTrial ? null : CurrentLevelHl,
            Waveform = WaveformType.Pulsed,
            DurationMs = 3000,
            CatchTrial = shouldCatchTrial,
            PlayedAt = DateTime.UtcNow
        };

        StimulusPrepared?.Invoke(this, stimulus);
        return stimulus;
    }

    public void RecordSpontaneousFalsePositive()
    {
        TotalCatchTrials++;
        FalsePositiveCount++;
        var eventSpontaneous = new StimulusEvent
        {
            SessionId = CurrentSession.Id,
            SequenceNo = CurrentSession.StimulusEvents.Count + 1,
            Ear = CurrentEar,
            FrequencyHz = CurrentFrequencyHz,
            ToneLevelHl = null,
            Waveform = WaveformType.Pulsed,
            DurationMs = 0,
            CatchTrial = true,
            Response = true,
            PlayedAt = DateTime.UtcNow,
            InvalidReason = "靜音期間誤按反應 (Spontaneous False Positive)"
        };
        CurrentSession.StimulusEvents.Add(eventSpontaneous);
    }

    public bool RecordResponse(StimulusEvent stimulus, bool responded, int? latencyMs = null)
    {
        stimulus.Response = responded;
        stimulus.ResponseLatencyMs = latencyMs;
        CurrentSession.StimulusEvents.Add(stimulus);

        if (stimulus.CatchTrial)
        {
            if (responded)
            {
                FalsePositiveCount++;
            }
            // Catch trial does not change current tone level
            return false;
        }

        // Check if threshold is reached for current frequency
        var eventsForFreq = CurrentSession.StimulusEvents
            .Where(e => e.Ear == CurrentEar && e.FrequencyHz == CurrentFrequencyHz && !e.CatchTrial)
            .ToList();

        if (_protocol.EvaluateThreshold(eventsForFreq, out double? threshold, out bool noResponse))
        {
            var result = new ThresholdResult
            {
                SessionId = CurrentSession.Id,
                Ear = CurrentEar,
                FrequencyHz = CurrentFrequencyHz,
                ThresholdHl = threshold,
                NoResponse = noResponse,
                Reliability = CalculateCurrentReliability(),
                AcceptedAt = DateTime.UtcNow
            };

            CurrentSession.ThresholdResults.Add(result);
            ThresholdDetermined?.Invoke(this, result);

            // Check 1000 Hz retest
            if (CurrentFrequencyHz == 1000)
            {
                if (!Initial1000HzThreshold.HasValue)
                {
                    Initial1000HzThreshold = threshold;
                }
                else if (!Retest1000HzThreshold.HasValue)
                {
                    Retest1000HzThreshold = threshold;
                    if (Initial1000HzThreshold.HasValue && Retest1000HzThreshold.HasValue)
                    {
                        double diff = Math.Abs(Initial1000HzThreshold.Value - Retest1000HzThreshold.Value);
                        if (diff > 5.0)
                        {
                            RetestWarningTriggered = true;
                            CurrentSession.OverallReliability = ReliabilityRating.Questionable;
                        }
                    }
                }
            }

            AdvanceToNextFrequencyOrEar();
            return true;
        }

        // Apply 10-down / 5-up stepping
        bool isAscending = eventsForFreq.Count > 1 && stimulus.ToneLevelHl >= eventsForFreq[^2].ToneLevelHl;
        CurrentLevelHl = _protocol.CalculateNextLevel(CurrentLevelHl, responded, isAscending);

        return false;
    }

    private void AdvanceToNextFrequencyOrEar()
    {
        CurrentFrequencyIndex++;
        if (CurrentFrequencyIndex < _protocol.FrequencySequence.Count)
        {
            // Reset starting presentation level for next frequency (typically 30 dB HL or near previous)
            CurrentLevelHl = 30.0;
        }
        else
        {
            // Finished current ear sequence
            if (TestBothEars && CurrentEar == FirstTestedEar)
            {
                // Switch to second ear
                CurrentEar = FirstTestedEar == Ear.Left ? Ear.Right : Ear.Left;
                CurrentFrequencyIndex = 0;
                CurrentLevelHl = 30.0;
            }
            else
            {
                // Test complete: clamp index to last valid frequency so property bindings don't throw
                CurrentFrequencyIndex = Math.Max(0, _protocol.FrequencySequence.Count - 1);
                SetState(TestProgressState.Completed);
                CurrentSession.CompletedAt = DateTime.UtcNow;
                CurrentSession.OverallReliability = CalculateCurrentReliability();
            }
        }
    }

    public ReliabilityRating CalculateCurrentReliability()
    {
        if (RetestWarningTriggered)
        {
            return ReliabilityRating.Questionable;
        }
        if (TotalCatchTrials > 0)
        {
            double falsePositiveRatio = (double)FalsePositiveCount / TotalCatchTrials;
            if (falsePositiveRatio > 0.33)
            {
                return ReliabilityRating.Poor;
            }
            if (falsePositiveRatio > 0.15)
            {
                return ReliabilityRating.Fair;
            }
        }
        return ReliabilityRating.Good;
    }

    public void ManualOverrideThreshold(Ear ear, int frequencyHz, double? newThresholdHl, bool noResponse, string operatorId, string reason)
    {
        if (newThresholdHl.HasValue && !_protocol.IsValidLevel(newThresholdHl.Value))
        {
            throw new ArgumentException($"Invalid level {newThresholdHl} not in whitelist.");
        }

        var existing = CurrentSession.ThresholdResults.FirstOrDefault(r => r.Ear == ear && r.FrequencyHz == frequencyHz);
        string beforeValue = existing != null ? existing.DisplayValue : "None";

        if (existing != null)
        {
            existing.ThresholdHl = newThresholdHl;
            existing.NoResponse = noResponse;
            existing.AcceptedBy = operatorId;
            existing.AcceptedAt = DateTime.UtcNow;
            existing.Notes += $" [Manual override by {operatorId}: {reason}]";
        }
        else
        {
            existing = new ThresholdResult
            {
                SessionId = CurrentSession.Id,
                Ear = ear,
                FrequencyHz = frequencyHz,
                ThresholdHl = newThresholdHl,
                NoResponse = noResponse,
                AcceptedBy = operatorId,
                AcceptedAt = DateTime.UtcNow,
                Notes = $"Manual entry by {operatorId}: {reason}"
            };
            CurrentSession.ThresholdResults.Add(existing);
        }

        var log = new AuditLog
        {
            Actor = operatorId,
            Action = "ManualOverrideThreshold",
            EntityType = nameof(ThresholdResult),
            EntityId = $"{ear}_{frequencyHz}Hz",
            Before = beforeValue,
            After = existing.DisplayValue,
            Timestamp = DateTime.UtcNow
        };
        AuditLogs.Add(log);
    }

    public bool RollbackLastStep(string operatorId = "Operator")
    {
        if (CurrentSession.StimulusEvents.Count == 0) return false;

        var lastEvent = CurrentSession.StimulusEvents[^1];
        CurrentSession.StimulusEvents.RemoveAt(CurrentSession.StimulusEvents.Count - 1);

        if (lastEvent.ToneLevelHl.HasValue)
        {
            CurrentLevelHl = lastEvent.ToneLevelHl.Value;
        }

        var log = new AuditLog
        {
            Actor = operatorId,
            Action = "RollbackLastStep",
            EntityType = nameof(StimulusEvent),
            EntityId = lastEvent.Id,
            Before = $"Seq {lastEvent.SequenceNo}, {lastEvent.Ear}, {lastEvent.FrequencyHz}Hz, Level: {lastEvent.ToneLevelHl}dB",
            After = "Removed",
            Timestamp = DateTime.UtcNow
        };
        AuditLogs.Add(log);
        return true;
    }

    private void SetState(TestProgressState newState)
    {
        State = newState;
        StateChanged?.Invoke(this, newState);
    }
}
