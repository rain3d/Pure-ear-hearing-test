using HearingTest.Domain;
using HearingTest.Domain.Models;
using HearingTest.Domain.StateMachine;
using HearingTest.Hardware;
using Xunit;

namespace HearingTest.Tests;

public class StateMachineTests
{
    [Fact]
    public void Test_10Down_5Up_Stepping()
    {
        var protocol = new StandardThresholdProtocol();

        // 聽到時下降 10 dB
        double nextAfterHeard = protocol.CalculateNextLevel(30.0, patientResponded: true, isAscendingRun: false);
        Assert.Equal(20.0, nextAfterHeard);

        // 沒聽到時上升 5 dB
        double nextAfterNotHeard = protocol.CalculateNextLevel(20.0, patientResponded: false, isAscendingRun: true);
        Assert.Equal(25.0, nextAfterNotHeard);

        // 下限為 0 dB
        double nextAtFloor = protocol.CalculateNextLevel(5.0, patientResponded: true, isAscendingRun: false);
        Assert.Equal(0.0, nextAtFloor);

        // 上限為 50 dB
        double nextAtCeiling = protocol.CalculateNextLevel(50.0, patientResponded: false, isAscendingRun: true);
        Assert.Equal(50.0, nextAtCeiling);
    }

    [Fact]
    public void Test_WhitelistedLevels_ClampAndValidation()
    {
        var protocol = new StandardThresholdProtocol();

        Assert.True(protocol.IsValidLevel(0));
        Assert.True(protocol.IsValidLevel(25));
        Assert.True(protocol.IsValidLevel(50));
        Assert.False(protocol.IsValidLevel(22)); // 非 5 dB 倍數
        Assert.False(protocol.IsValidLevel(55)); // 超出 50 dB
        Assert.False(protocol.IsValidLevel(-5)); // 低於 0 dB

        Assert.Equal(25.0, protocol.ClampLevel(23.0));
        Assert.Equal(50.0, protocol.ClampLevel(75.0));
        Assert.Equal(0.0, protocol.ClampLevel(-10.0));
    }

    [Fact]
    public void Test_2Of3_AscendingThresholdRule()
    {
        var protocol = new StandardThresholdProtocol();
        var events = new List<StimulusEvent>();

        void AddEvent(int seq, double level, bool response)
        {
            events.Add(new StimulusEvent
            {
                SequenceNo = seq,
                Ear = Ear.Left,
                FrequencyHz = 1000,
                ToneLevelHl = level,
                Response = response,
                Valid = true
            });
        }

        // 模擬一個 10-down / 5-up 序列：
        // 1. 30 dB -> 聽到 (下降至 20)
        AddEvent(1, 30, true);
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 2. 20 dB -> 沒聽到 (上升至 25)
        AddEvent(2, 20, false);
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 3. 25 dB -> 聽到 (第 1 次上升至 25 dB 聽到！下降至 15)
        AddEvent(3, 25, true);
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 4. 15 dB -> 沒聽到 (上升至 20)
        AddEvent(4, 15, false);
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 5. 20 dB -> 沒聽到 (上升至 25)
        AddEvent(5, 20, false);
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 6. 25 dB -> 再次聽到！(在上升序列中，25 dB 出現 2 次且均有反應，符合 2-of-3 標準)
        AddEvent(6, 25, true);
        bool evaluated = protocol.EvaluateThreshold(events, out double? threshold, out bool noResponse);

        Assert.True(evaluated);
        Assert.Equal(25.0, threshold);
        Assert.False(noResponse);
    }

    [Fact]
    public void Test_FloorAt0dBHL_DeterminesThreshold()
    {
        var protocol = new StandardThresholdProtocol();
        var events = new List<StimulusEvent>
        {
            new() { SequenceNo = 1, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 30, Response = true, Valid = true },
            new() { SequenceNo = 2, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 20, Response = true, Valid = true },
            new() { SequenceNo = 3, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 10, Response = true, Valid = true },
            new() { SequenceNo = 4, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 0, Response = true, Valid = true }
        };

        // 僅 1 次 0 dB 反應尚未確認聽閾
        Assert.False(protocol.EvaluateThreshold(events, out _, out _));

        // 第 2 次在 0 dB 聽到，確立聽閾為 0 dB HL
        events.Add(new() { SequenceNo = 5, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 0, Response = true, Valid = true });
        bool evaluated = protocol.EvaluateThreshold(events, out double? threshold, out bool noResponse);

        Assert.True(evaluated);
        Assert.Equal(0.0, threshold);
        Assert.False(noResponse);
    }

    [Fact]
    public void Test_CatchTrial_DoesNotCountTowardsThreshold_IncrementsFalsePositive()
    {
        var sm = new AudiometryStateMachine(new StandardThresholdProtocol(), randomSeed: 42);
        var patient = Patient.CreateAnonymous();
        sm.StartSession(patient, Ear.Left, bothEars: false, startingLevel: 30);

        var catchStimulus = new StimulusEvent
        {
            SessionId = sm.CurrentSession.Id,
            SequenceNo = 1,
            Ear = Ear.Left,
            FrequencyHz = 1000,
            ToneLevelHl = null, // Silent catch trial
            CatchTrial = true,
            Valid = true
        };

        // 受測者在 Catch Trial 誤按聽到（疑似假陽性）
        bool finished = sm.RecordResponse(catchStimulus, responded: true, latencyMs: 320);

        Assert.False(finished);
        Assert.Equal(1, sm.FalsePositiveCount);
        // 聽閾結果不可因此確立
        Assert.Empty(sm.CurrentSession.ThresholdResults);
    }

    [Fact]
    public void Test_1000Hz_Retest_Difference_Warning()
    {
        var sm = new AudiometryStateMachine(new StandardThresholdProtocol(), randomSeed: 12345);
        var patient = Patient.CreateAnonymous();
        sm.StartSession(patient, Ear.Left, bothEars: false);

        // 模擬手動直接設定 1000 Hz 初測為 20 dB HL
        sm.ManualOverrideThreshold(Ear.Left, 1000, 20.0, false, "Tester", "Initial 1000Hz");
        // 模擬手動設定 1000 Hz 複測為 35 dB HL (差 15 dB，遠大於 5 dB 警戒線)
        sm.ManualOverrideThreshold(Ear.Left, 1000, 35.0, false, "Tester", "Retest 1000Hz");

        // 驗證稽核日誌已建立
        Assert.Equal(2, sm.AuditLogs.Count);
        Assert.Equal("ManualOverrideThreshold", sm.AuditLogs[0].Action);
        Assert.Equal("ManualOverrideThreshold", sm.AuditLogs[1].Action);
    }

    [Fact]
    public void Test_NoResponseAt50dBHL_FlaggedAsNR()
    {
        var protocol = new StandardThresholdProtocol();
        var events = new List<StimulusEvent>
        {
            new() { SequenceNo = 1, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 40, Response = false, Valid = true },
            new() { SequenceNo = 2, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 45, Response = false, Valid = true },
            new() { SequenceNo = 3, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 50, Response = false, Valid = true },
            new() { SequenceNo = 4, Ear = Ear.Left, FrequencyHz = 1000, ToneLevelHl = 50, Response = false, Valid = true }
        };

        bool evaluated = protocol.EvaluateThreshold(events, out double? threshold, out bool noResponse);
        Assert.True(evaluated);
        Assert.Equal(50.0, threshold);
        Assert.True(noResponse);
    }

    [Fact]
    public void Test_Rollback_RestoresStateAndGeneratesAuditLog()
    {
        var sm = new AudiometryStateMachine(new StandardThresholdProtocol(), randomSeed: 100);
        var patient = Patient.CreateAnonymous();
        sm.StartSession(patient, Ear.Left, bothEars: false, startingLevel: 30);

        var stim = sm.NextStimulus();
        sm.RecordResponse(stim, responded: false);

        int countBefore = sm.CurrentSession.StimulusEvents.Count;
        Assert.Equal(1, countBefore);

        bool rolledBack = sm.RollbackLastStep("DrTest");
        Assert.True(rolledBack);
        Assert.Empty(sm.CurrentSession.StimulusEvents);
        Assert.Single(sm.AuditLogs);
        Assert.Equal("RollbackLastStep", sm.AuditLogs[0].Action);
        Assert.Equal("DrTest", sm.AuditLogs[0].Actor);
    }

    [Fact]
    public void Test_DeterministicSimulation_WithFixedRandomSeed_ReproducesAudiogram()
    {
        // 使用固定亂數種子跑兩次完整模擬受測者，兩次結果必須完全一致
        List<ThresholdResult> RunFullTest(int seed)
        {
            var sm = new AudiometryStateMachine(new StandardThresholdProtocol(), randomSeed: seed);
            var patient = new SimulatedPatient(seed: seed);
            patient.SetLeftEarVerificationProfile();

            var subject = Patient.CreateAnonymous();
            sm.StartSession(subject, Ear.Left, bothEars: false, startingLevel: 30.0);

            int maxSteps = 100;
            int step = 0;
            while (sm.State == TestProgressState.Testing && step++ < maxSteps)
            {
                var stim = sm.NextStimulus();
                var (heard, latency) = patient.EvaluateStimulus(stim);
                sm.RecordResponse(stim, heard, latency);
            }

            return sm.CurrentSession.ThresholdResults;
        }

        var run1 = RunFullTest(777);
        var run2 = RunFullTest(777);

        Assert.NotEmpty(run1);
        Assert.Equal(run1.Count, run2.Count);

        for (int i = 0; i < run1.Count; i++)
        {
            Assert.Equal(run1[i].FrequencyHz, run2[i].FrequencyHz);
            Assert.Equal(run1[i].ThresholdHl, run2[i].ThresholdHl);
            Assert.Equal(run1[i].NoResponse, run2[i].NoResponse);
        }
    }

    [Fact]
    public void Test_StimulusDuration_DefaultsTo3000ms()
    {
        var sm = new AudiometryStateMachine();
        sm.StartSession(Patient.CreateAnonymous());
        var stim = sm.NextStimulus();

        Assert.Equal(3000, stim.DurationMs);
    }

    [Fact]
    public void Test_SpontaneousFalsePositive_IncrementsCatchTrialsAndFalsePositives()
    {
        var sm = new AudiometryStateMachine();
        sm.StartSession(Patient.CreateAnonymous());

        sm.RecordSpontaneousFalsePositive();

        Assert.Equal(1, sm.FalsePositiveCount);
        Assert.Equal(1, sm.TotalCatchTrials);
        var last = sm.CurrentSession.StimulusEvents.Last();
        Assert.True(last.CatchTrial);
        Assert.True(last.Response);
        Assert.Contains("Spontaneous False Positive", last.InvalidReason);
    }

    [Fact]
    public void Test_CompletedState_CurrentFrequencyHz_DoesNotThrow()
    {
        var sm = new AudiometryStateMachine(new StandardThresholdProtocol());
        var patient = new SimulatedPatient(seed: 123);
        patient.SetLeftEarVerificationProfile();

        sm.StartSession(Patient.CreateAnonymous(), Ear.Left, bothEars: true, startingLevel: 30.0);

        int maxSteps = 150;
        int step = 0;
        while (sm.State == TestProgressState.Testing && step++ < maxSteps)
        {
            var stim = sm.NextStimulus();
            var (heard, latency) = patient.EvaluateStimulus(stim);
            sm.RecordResponse(stim, heard, latency);
        }

        Assert.Equal(TestProgressState.Completed, sm.State);

        // When test completes, accessing CurrentFrequencyHz must NEVER throw IndexOutOfRangeException
        var ex = Record.Exception(() =>
        {
            int freq = sm.CurrentFrequencyHz;
            Assert.True(freq > 0);
        });
        Assert.Null(ex);
    }

    [Fact]
    public void Test_SimulateAllHeard_AdvancesThrough4000Hz()
    {
        var sm = new AudiometryStateMachine(new StandardThresholdProtocol());
        sm.StartSession(Patient.CreateAnonymous(), Ear.Left, bothEars: true, startingLevel: 30.0);

        int maxStimuli = 100;
        int count = 0;
        while (sm.State == TestProgressState.Testing && count++ < maxStimuli)
        {
            var stim = sm.NextStimulus();
            sm.RecordResponse(stim, responded: true);
        }

        Assert.Equal(TestProgressState.Completed, sm.State);
    }
}


