using HearingTest.Domain.Interfaces;

namespace HearingTest.Domain.StateMachine;

/// <summary>
/// 標準對側窄頻遮蔽協定 (依據 ISO 8253-1 及臨床 Plateau 原則)
/// </summary>
public class StandardMaskingProtocol : IMaskingProtocol
{
    public string Version => "1.0.0-ISO8253";

    /// <summary>
    /// 耳罩式耳機保守耳間衰減 (Interaural Attenuation, IA)，臨床標準通常為 40 dB
    /// </summary>
    public const double InterauralAttenuationDb = 40.0;

    /// <summary>
    /// 安全輸出硬上限 50 dB HL
    /// </summary>
    public const double SafetyCeilingHl = 50.0;

    /// <summary>
    /// 判定是否達到對側遮蔽提示門檻：測試音強度與非測試耳聽閾差異 >= 40 dB
    /// </summary>
    public bool ShouldPromptMasking(
        Ear testEar,
        int frequencyHz,
        double testToneHl,
        double? contralateralAirThreshold,
        double? contralateralBoneThreshold)
    {
        double referenceThreshold = contralateralBoneThreshold ?? contralateralAirThreshold ?? 0.0;
        return (testToneHl - referenceThreshold) >= InterauralAttenuationDb;
    }

    /// <summary>
    /// 計算初始有效遮蔽量 (Initial Masking Level)
    /// 公式：非測試耳聽閾 + 10 dB 安全餘量 (或估計有效遮蔽)，對齊至 5 dB 步階並限制於 [0, 50] dB HL
    /// </summary>
    public double CalculateInitialMaskingLevel(Ear nonTestEar, int frequencyHz, double testToneHl)
    {
        double estimatedLevel = Math.Max(10.0, testToneHl - InterauralAttenuationDb + 10.0);
        double rounded = Math.Round(estimatedLevel / 5.0) * 5.0;
        return Math.Clamp(rounded, 0.0, SafetyCeilingHl);
    }
}
