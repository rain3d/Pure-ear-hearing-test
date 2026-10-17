using HearingTest.Domain.Models;

namespace HearingTest.Domain.Interfaces;

public interface IThresholdProtocol
{
    string Name { get; }
    string Version { get; }
    IReadOnlyList<int> FrequencySequence { get; }
    double MinLevelHl { get; }
    double MaxLevelHl { get; }
    double StepSizeDb { get; }

    bool IsValidFrequency(int frequencyHz);
    bool IsValidLevel(double levelHl);
    double ClampLevel(double levelHl);

    /// <summary>
    /// 計算下一刺激強度（10-down / 5-up 規則）
    /// </summary>
    double CalculateNextLevel(double currentLevel, bool patientResponded, bool isAscendingRun);

    /// <summary>
    /// 評估是否達到 2-of-3 上升序列聽閾標準
    /// </summary>
    bool EvaluateThreshold(IEnumerable<StimulusEvent> eventsForFrequency, out double? thresholdHl, out bool noResponse);
}
