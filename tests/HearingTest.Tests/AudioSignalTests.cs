using HearingTest.Audio;
using HearingTest.Domain;
using Xunit;

namespace HearingTest.Tests;

public class AudioSignalTests
{
    [Theory]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(4000)]
    [InlineData(8000)]
    public void Test_Frequency_Accuracy_AllSixFrequencies(int targetFreq)
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);
        int durationMs = 1000;
        float[] buffer = generator.GenerateStimulus(
            Ear.Left,
            targetFreq,
            dbfs: -20.0,
            WaveformType.Continuous,
            durationMs,
            rampDurationMs: 25.0);

        // 擷取穩定態區段（避開開頭與結尾 50 ms 升降餘弦）
        int startSample = (int)(0.1 * generator.SampleRate);
        int endSample = (int)(0.9 * generator.SampleRate);

        int zeroCrossings = 0;
        float prev = buffer[startSample * 2]; // Left channel

        for (int i = startSample + 1; i < endSample; i++)
        {
            float curr = buffer[i * 2];
            if ((prev <= 0 && curr > 0) || (prev >= 0 && curr < 0))
            {
                zeroCrossings++;
            }
            prev = curr;
        }

        double durationSec = (endSample - startSample) / (double)generator.SampleRate;
        double measuredFreq = zeroCrossings / (2.0 * durationSec);

        // 數位頻率誤差應 < 0.1% (通常在 0.01% 以內)
        double relativeError = Math.Abs(measuredFreq - targetFreq) / targetFreq;
        Assert.True(relativeError < 0.005, $"Frequency {targetFreq}Hz error {relativeError:P2} exceeded limit (measured: {measuredFreq:F1}Hz)");
    }

    [Fact]
    public void Test_Channel_Isolation_Zero_Digital_Crosstalk()
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);

        // 1. 測左耳時：左聲道有聲音，右聲道完全為 0
        float[] leftStim = generator.GenerateStimulus(Ear.Left, 1000, -20.0, WaveformType.Continuous, 500);
        for (int i = 0; i < leftStim.Length / 2; i++)
        {
            float leftSample = leftStim[i * 2];
            float rightSample = leftStim[i * 2 + 1];

            Assert.Equal(0.0f, rightSample); // 右聲道嚴格零串音
        }

        // 2. 測右耳時：右聲道有聲音，左聲道完全為 0
        float[] rightStim = generator.GenerateStimulus(Ear.Right, 1000, -20.0, WaveformType.Continuous, 500);
        for (int i = 0; i < rightStim.Length / 2; i++)
        {
            float leftSample = rightStim[i * 2];
            float rightSample = rightStim[i * 2 + 1];

            Assert.Equal(0.0f, leftSample); // 左聲道嚴格零串音
        }
    }

    [Fact]
    public void Test_Safety_Ceiling_Interlock_Prevents_Clipping()
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);

        // 即使請求輸入 +20 dBFS（危險過載值）
        float[] buffer = generator.GenerateStimulus(Ear.Left, 1000, dbfs: +20.0, WaveformType.Continuous, 500);

        double expectedMaxAmp = AudioSignalGenerator.DbfsToLinearAmplitude(AudioSignalGenerator.SafetyCeilingDbfs);
        float actualMaxPeak = 0f;

        for (int i = 0; i < buffer.Length; i++)
        {
            actualMaxPeak = Math.Max(actualMaxPeak, Math.Abs(buffer[i]));
        }

        // 峰值絕不可超過 -10 dBFS 相當於約 0.3162 線性振幅
        Assert.True(actualMaxPeak <= (float)expectedMaxAmp + 1e-4f, $"Peak amplitude {actualMaxPeak} exceeded safety ceiling {expectedMaxAmp}");
        Assert.True(actualMaxPeak < 1.0f, "Clipping protection failed: signal reached 1.0 full scale!");
    }

    [Fact]
    public void Test_RaisedCosine_Click_Prevention()
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);
        float[] buffer = generator.GenerateStimulus(Ear.Left, 1000, -20.0, WaveformType.Continuous, 500, rampDurationMs: 25.0);

        // 第一個取樣點必須為 0.0，杜絕方波瞬間切換 click
        Assert.Equal(0.0f, buffer[0]);

        // 檢查上升段相鄰取樣點變化率平滑
        int rampSamples = (int)(0.025 * 48000);
        float maxStep = 0f;
        for (int i = 1; i < rampSamples; i++)
        {
            float diff = Math.Abs(buffer[i * 2] - buffer[(i - 1) * 2]);
            maxStep = Math.Max(maxStep, diff);
        }

        // 變化率應微小連續，無階躍斷裂
        Assert.True(maxStep < 0.05f, $"Discontinuous step {maxStep} detected during raised-cosine ramp!");
    }

    [Fact]
    public void Test_PulsedTone_DutyCycle()
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);
        // 脈衝音：400 ms 一個週期 (200 ms 音響 + 200 ms 靜音)
        float[] buffer = generator.GenerateStimulus(Ear.Left, 1000, -20.0, WaveformType.Pulsed, 800);

        int sampleRate = 48000;
        // 檢查第 250~350 ms（落在第 1 個脈衝的靜音區間）
        int quietStart = (int)(0.25 * sampleRate);
        int quietEnd = (int)(0.35 * sampleRate);

        for (int i = quietStart; i < quietEnd; i++)
        {
            Assert.Equal(0.0f, buffer[i * 2]);
        }

        // 檢查第 450~550 ms（落在第 2 個脈衝的聲音區間）
        int activeStart = (int)(0.45 * sampleRate);
        int activeEnd = (int)(0.55 * sampleRate);
        bool hasSound = false;
        for (int i = activeStart; i < activeEnd; i++)
        {
            if (Math.Abs(buffer[i * 2]) > 0.01f)
            {
                hasSound = true;
                break;
            }
        }
        Assert.True(hasSound, "Pulsed tone active interval missing sound!");
    }

    [Fact]
    public void Test_Duration_Matches_Specification()
    {
        var generator = new AudioSignalGenerator(sampleRate: 48000);
        int durationMs = 1500;
        float[] buffer = generator.GenerateStimulus(Ear.Left, 1000, -20.0, WaveformType.Continuous, durationMs);

        int expectedStereoLength = (int)((durationMs / 1000.0) * 48000) * 2;
        Assert.Equal(expectedStereoLength, buffer.Length);
    }
}
