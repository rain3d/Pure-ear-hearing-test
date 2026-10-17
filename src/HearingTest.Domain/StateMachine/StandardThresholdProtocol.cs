using HearingTest.Domain.Interfaces;
using HearingTest.Domain.Models;

namespace HearingTest.Domain.StateMachine;

public class StandardThresholdProtocol : IThresholdProtocol
{
    public string Name => "ISO 8253-1 / ASHA 10-Down 5-Up";
    public string Version => "1.0.0-SixFrequency";

    public static readonly int[] StandardFrequencies = [1000, 2000, 4000, 8000, 1000, 500, 250];
    public static readonly double[] WhitelistedLevels = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50];

    public IReadOnlyList<int> FrequencySequence => StandardFrequencies;
    public double MinLevelHl => 0.0;
    public double MaxLevelHl => 50.0;
    public double StepSizeDb => 5.0;

    public bool IsValidFrequency(int frequencyHz)
    {
        return frequencyHz is 250 or 500 or 1000 or 2000 or 4000 or 8000;
    }

    public bool IsValidLevel(double levelHl)
    {
        return WhitelistedLevels.Contains(levelHl);
    }

    public double ClampLevel(double levelHl)
    {
        if (levelHl < MinLevelHl) return MinLevelHl;
        if (levelHl > MaxLevelHl) return MaxLevelHl;
        // Snap to nearest 5 dB step
        double rounded = Math.Round(levelHl / StepSizeDb) * StepSizeDb;
        return Math.Clamp(rounded, MinLevelHl, MaxLevelHl);
    }

    public double CalculateNextLevel(double currentLevel, bool patientResponded, bool isAscendingRun)
    {
        if (patientResponded)
        {
            // 聽到：降低 10 dB
            return Math.Max(MinLevelHl, currentLevel - 10.0);
        }
        else
        {
            // 沒聽到：增加 5 dB
            return Math.Min(MaxLevelHl, currentLevel + 5.0);
        }
    }

    public bool EvaluateThreshold(IEnumerable<StimulusEvent> eventsForFrequency, out double? thresholdHl, out bool noResponse)
    {
        thresholdHl = null;
        noResponse = false;

        var validEvents = eventsForFrequency
            .Where(e => e.Valid && !e.CatchTrial && e.ToneLevelHl.HasValue)
            .OrderBy(e => e.SequenceNo)
            .ToList();

        if (validEvents.Count == 0) return false;

        // Check for No Response at 50 dB HL (maximum ceiling)
        // If at 50 dB HL, patient has failed to respond on 2 or more presentations without any subsequent heard response
        int presentationsAt50 = validEvents.Count(e => e.ToneLevelHl == MaxLevelHl);
        int responsesAt50 = validEvents.Count(e => e.ToneLevelHl == MaxLevelHl && e.Response);
        if (presentationsAt50 >= 2 && responsesAt50 == 0)
        {
            var lastEvent = validEvents.Last();
            if (lastEvent.ToneLevelHl == MaxLevelHl && !lastEvent.Response)
            {
                thresholdHl = MaxLevelHl;
                noResponse = true;
                return true;
            }
        }

        // Check for Minimum Floor at 0 dB HL
        // When at MinLevelHl (0 dB HL), descending lower is impossible.
        // If patient responds to 2 or more presentations at MinLevelHl, threshold is established at MinLevelHl.
        int responsesAtMin = validEvents.Count(e => e.ToneLevelHl == MinLevelHl && e.Response);
        if (responsesAtMin >= 2)
        {
            var lastEvent = validEvents.Last();
            if (lastEvent.ToneLevelHl == MinLevelHl && lastEvent.Response)
            {
                thresholdHl = MinLevelHl;
                noResponse = false;
                return true;
            }
        }

        // Identify ascending presentations
        // In Hughson-Westlake: an ascending trial is one that follows a "not heard" or a level increase,
        // or any presentation where current level > previous level.
        // Also the first presentation after down-10 recovery is tested upwards.
        // We count for each level: total ascending presentations and total positive responses on ascending trials.
        var levelStats = new Dictionary<double, (int Presentations, int Responses)>();

        for (int i = 0; i < validEvents.Count; i++)
        {
            var current = validEvents[i];
            double level = current.ToneLevelHl!.Value;

            // Determine if this presentation was ascending
            // An ascending presentation occurs when:
            // 1. i > 0 and current.Level > previous.Level (direct 5-up step)
            // 2. OR i > 0 and previous was non-response and current.Level == previous.Level
            bool isAscending = false;
            if (i > 0)
            {
                var prev = validEvents[i - 1];
                if (current.ToneLevelHl > prev.ToneLevelHl)
                {
                    isAscending = true;
                }
                else if (current.ToneLevelHl == prev.ToneLevelHl && !prev.Response)
                {
                    isAscending = true;
                }
            }

            if (isAscending)
            {
                if (!levelStats.TryGetValue(level, out var stat))
                {
                    stat = (0, 0);
                }
                stat.Presentations++;
                if (current.Response)
                {
                    stat.Responses++;
                }
                levelStats[level] = stat;

                // 2-of-3 threshold rule:
                // If at this level, we have at least 2 responses out of <= 3 ascending presentations
                if (stat.Responses >= 2 && stat.Presentations <= 3)
                {
                    thresholdHl = level;
                    noResponse = false;
                    return true;
                }
                // Also if stat.Responses >= 2 even if stat.Presentations is 4 (e.g. 2 out of 3 or 3 out of 4)
                if (stat.Responses >= 2 && (double)stat.Responses / stat.Presentations >= 0.5)
                {
                    thresholdHl = level;
                    noResponse = false;
                    return true;
                }
            }
        }

        return false;
    }
}
