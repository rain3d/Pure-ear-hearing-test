using HearingTest.Domain;
using HearingTest.Domain.Models;

namespace HearingTest.Hardware;

public class SimulatedPatient
{
    private readonly Random _random;
    public Dictionary<(Ear Ear, int FrequencyHz), double> TrueThresholds { get; } = new();
    public double FalsePositiveRate { get; set; } = 0.05;
    public int MeanLatencyMs { get; set; } = 400;

    public SimulatedPatient(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        SetDefaultNormalHearing();
    }

    public void SetDefaultNormalHearing()
    {
        int[] freqs = [250, 500, 1000, 2000, 4000, 8000];
        foreach (var f in freqs)
        {
            TrueThresholds[(Ear.Left, f)] = 15.0;
            TrueThresholds[(Ear.Right, f)] = 15.0;
        }
    }

    /// <summary>
    /// 套用規格中記錄之使用者個人複測預估值
    /// 500Hz: 20dB, 1000Hz: 40dB, 2000Hz: 30dB
    /// </summary>
    public void SetLeftEarVerificationProfile()
    {
        TrueThresholds[(Ear.Left, 250)] = 20.0;
        TrueThresholds[(Ear.Left, 500)] = 20.0;
        TrueThresholds[(Ear.Left, 1000)] = 40.0;
        TrueThresholds[(Ear.Left, 2000)] = 30.0;
        TrueThresholds[(Ear.Left, 4000)] = 25.0;
        TrueThresholds[(Ear.Left, 8000)] = 35.0;

        // 右耳設定為正常對照
        int[] freqs = [250, 500, 1000, 2000, 4000, 8000];
        foreach (var f in freqs)
        {
            TrueThresholds[(Ear.Right, f)] = 15.0;
        }
    }

    /// <summary>
    /// 模擬受測者對特定刺激之反應（心理物理學常態心理計量累積常態/邏輯斯曲線）
    /// </summary>
    public (bool Responded, int LatencyMs) EvaluateStimulus(StimulusEvent stimulus)
    {
        if (stimulus.CatchTrial || !stimulus.ToneLevelHl.HasValue)
        {
            // Catch trial: 僅依假陽性機率觸發
            bool fp = _random.NextDouble() < FalsePositiveRate;
            int latency = fp ? (int)(MeanLatencyMs + (_random.NextDouble() - 0.5) * 200) : 0;
            return (fp, Math.Max(150, latency));
        }

        if (!TrueThresholds.TryGetValue((stimulus.Ear, stimulus.FrequencyHz), out double trueThreshold))
        {
            trueThreshold = 20.0; // 預設
        }

        double level = stimulus.ToneLevelHl.Value;
        double diff = level - trueThreshold;

        // 心理計量函數：當 level == threshold 時，偵測機率為 50%
        // 當 level > threshold + 5 時，偵測機率 > 95%
        // 當 level < threshold - 5 時，偵測機率 < 5%
        double k = 0.8; // 斜率陡度
        double prob = 1.0 / (1.0 + Math.Exp(-k * diff));

        // 納入隨機假陽性 baseline
        prob = Math.Max(prob, FalsePositiveRate);

        bool heard = _random.NextDouble() < prob;
        int responseLatency = heard
            ? (int)(MeanLatencyMs + Math.Max(0, 150 - diff * 15) + (_random.NextDouble() - 0.5) * 120)
            : 0;

        return (heard, Math.Max(180, responseLatency));
    }
}
