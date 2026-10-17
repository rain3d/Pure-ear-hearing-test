using HearingTest.Audio;
using HearingTest.Domain;
using HearingTest.Domain.StateMachine;
using Xunit;

namespace HearingTest.Tests;

public class MaskingTests
{
    private readonly AudioSignalGenerator _generator = new(48000);
    private readonly StandardMaskingProtocol _maskingProtocol = new();

    [Fact]
    public void StandardMaskingProtocol_ShouldPromptMasking_WhenDifferenceExceeds40Db()
    {
        // 45 dB HL vs 0 dB HL (差異 45 dB >= 40 dB IA) -> 應提示遮蔽
        bool shouldMask = _maskingProtocol.ShouldPromptMasking(
            testEar: Ear.Left,
            frequencyHz: 1000,
            testToneHl: 45.0,
            contralateralAirThreshold: 0.0,
            contralateralBoneThreshold: null);

        Assert.True(shouldMask);
    }

    [Fact]
    public void StandardMaskingProtocol_ShouldNotPromptMasking_WhenDifferenceBelow40Db()
    {
        // 30 dB HL vs 0 dB HL (差異 30 dB < 40 dB IA) -> 不提示遮蔽
        bool shouldMask = _maskingProtocol.ShouldPromptMasking(
            testEar: Ear.Left,
            frequencyHz: 1000,
            testToneHl: 30.0,
            contralateralAirThreshold: 0.0,
            contralateralBoneThreshold: null);

        Assert.False(shouldMask);
    }

    [Fact]
    public void StandardMaskingProtocol_CalculateInitialMaskingLevel_ClampsToSafeRange()
    {
        // 測試音 50 dB HL，計算遮蔽量應在安全範圍內 (<= 50 dB HL) 且以 5 dB 為步階
        double level = _maskingProtocol.CalculateInitialMaskingLevel(Ear.Right, 1000, 50.0);
        Assert.True(level is >= 0.0 and <= 50.0);
        Assert.Equal(0.0, level % 5.0);
    }

    [Fact]
    public void GenerateStimulusWithMasking_RoutesLeftToneAndRightMaskingCorrectly()
    {
        // 測左耳純音，對側右耳窄頻遮蔽
        float[] stereo = _generator.GenerateStimulusWithMasking(
            toneEar: Ear.Left,
            toneFrequencyHz: 1000,
            toneDbfs: -20.0,
            maskingEar: Ear.Right,
            maskingType: MaskingType.NarrowbandNoise,
            maskingDbfs: -25.0,
            waveform: WaveformType.Continuous,
            durationMs: 200);

        int totalSamplesPerChannel = stereo.Length / 2;
        double leftRms = 0;
        double rightRms = 0;

        for (int i = 0; i < totalSamplesPerChannel; i++)
        {
            float l = stereo[i * 2];
            float r = stereo[i * 2 + 1];
            leftRms += l * l;
            rightRms += r * r;
        }

        leftRms = Math.Sqrt(leftRms / totalSamplesPerChannel);
        rightRms = Math.Sqrt(rightRms / totalSamplesPerChannel);

        // 雙耳皆有訊號且不為零
        Assert.True(leftRms > 0.001, "Left ear (tone) should have audio energy.");
        Assert.True(rightRms > 0.001, "Right ear (masking) should have audio energy.");

        // 開頭與結尾平滑無 Click
        Assert.True(Math.Abs(stereo[0]) < 1e-4, "Left start should be ramped.");
        Assert.True(Math.Abs(stereo[1]) < 1e-4, "Right start should be ramped.");
    }

    [Fact]
    public void GenerateStimulusWithMasking_RoutesRightToneAndLeftMaskingCorrectly()
    {
        // 測右耳純音，對側左耳窄頻遮蔽
        float[] stereo = _generator.GenerateStimulusWithMasking(
            toneEar: Ear.Right,
            toneFrequencyHz: 2000,
            toneDbfs: -15.0,
            maskingEar: Ear.Left,
            maskingType: MaskingType.NarrowbandNoise,
            maskingDbfs: -20.0,
            waveform: WaveformType.Continuous,
            durationMs: 200);

        int totalSamplesPerChannel = stereo.Length / 2;
        double leftRms = 0;
        double rightRms = 0;

        for (int i = 0; i < totalSamplesPerChannel; i++)
        {
            leftRms += stereo[i * 2] * stereo[i * 2];
            rightRms += stereo[i * 2 + 1] * stereo[i * 2 + 1];
        }

        leftRms = Math.Sqrt(leftRms / totalSamplesPerChannel);
        rightRms = Math.Sqrt(rightRms / totalSamplesPerChannel);

        Assert.True(leftRms > 0.001, "Left ear (masking) should have energy.");
        Assert.True(rightRms > 0.001, "Right ear (tone) should have energy.");
    }

    [Fact]
    public void GenerateStimulusWithMasking_ThrowsWhenSameEarMasked()
    {
        // 嚴禁同側耳遮蔽（安全互鎖防呆）
        Assert.Throws<ArgumentException>(() =>
        {
            _generator.GenerateStimulusWithMasking(
                toneEar: Ear.Left,
                toneFrequencyHz: 1000,
                toneDbfs: -20.0,
                maskingEar: Ear.Left,
                maskingType: MaskingType.NarrowbandNoise,
                maskingDbfs: -20.0,
                waveform: WaveformType.Continuous,
                durationMs: 100);
        });
    }

    [Fact]
    public void GenerateStimulus_NoMasking_MaintainsZeroInOppositeChannel()
    {
        // 無遮蔽時，非測試耳嚴格為 0.0f
        float[] stereo = _generator.GenerateStimulus(
            ear: Ear.Left,
            frequencyHz: 1000,
            dbfs: -20.0,
            waveform: WaveformType.Continuous,
            durationMs: 100);

        int totalSamples = stereo.Length / 2;
        for (int i = 0; i < totalSamples; i++)
        {
            Assert.Equal(0.0f, stereo[i * 2 + 1]);
        }
    }
}
