using HearingTest.Domain;

namespace HearingTest.Audio;

public class AudioSignalGenerator
{
    public const int DefaultSampleRate = 48000;
    public const double SafetyCeilingDbfs = -10.0; // 工程測試安全硬上限
    public const double DefaultRampDurationMs = 25.0; // 臨床推薦 20~25 ms 無 Click 升降餘弦平滑

    public int SampleRate { get; }

    public AudioSignalGenerator(int sampleRate = DefaultSampleRate)
    {
        SampleRate = sampleRate;
    }

    /// <summary>
    /// 計算數位線性振幅（受安全硬上限保護）
    /// </summary>
    public static double DbfsToLinearAmplitude(double dbfs, double ceilingDbfs = SafetyCeilingDbfs)
    {
        double clampedDbfs = Math.Min(dbfs, ceilingDbfs);
        return Math.Pow(10.0, clampedDbfs / 20.0);
    }

    /// <summary>
    /// 計算線性振幅轉 dBFS
    /// </summary>
    public static double LinearAmplitudeToDbfs(double amplitude)
    {
        if (amplitude <= 1e-9) return -180.0;
        return 20.0 * Math.Log10(amplitude);
    }

    /// <summary>
    /// 生成立體聲交錯 (Interleaved L, R) 純音刺激緩衝區（非測試耳靜音）
    /// </summary>
    public float[] GenerateStimulus(
        Ear ear,
        int frequencyHz,
        double dbfs,
        WaveformType waveform,
        int durationMs,
        double rampDurationMs = DefaultRampDurationMs)
    {
        int totalSamplesPerChannel = (int)Math.Round((durationMs / 1000.0) * SampleRate);
        float[] stereoBuffer = new float[totalSamplesPerChannel * 2]; // Interleaved L, R

        double targetAmp = DbfsToLinearAmplitude(dbfs);
        if (targetAmp <= 0) return stereoBuffer;

        float[] toneMono = new float[totalSamplesPerChannel];
        if (waveform == WaveformType.Pulsed)
        {
            GeneratePulsedToneMono(toneMono, frequencyHz, targetAmp, durationMs, rampDurationMs);
        }
        else
        {
            GenerateContinuousToneMono(toneMono, frequencyHz, targetAmp, totalSamplesPerChannel, rampDurationMs);
        }

        // 映射至指定耳，另一耳保持嚴格靜音
        for (int i = 0; i < totalSamplesPerChannel; i++)
        {
            int leftIdx = i * 2;
            int rightIdx = i * 2 + 1;
            if (ear == Ear.Left)
            {
                stereoBuffer[leftIdx] = toneMono[i];
                stereoBuffer[rightIdx] = 0.0f;
            }
            else
            {
                stereoBuffer[leftIdx] = 0.0f;
                stereoBuffer[rightIdx] = toneMono[i];
            }
        }

        return stereoBuffer;
    }

    /// <summary>
    /// 生成立體聲刺激：測試耳輸出純音／脈衝音，非測試耳輸出對側窄頻遮蔽噪音
    /// </summary>
    public float[] GenerateStimulusWithMasking(
        Ear toneEar,
        int toneFrequencyHz,
        double toneDbfs,
        Ear maskingEar,
        MaskingType maskingType,
        double maskingDbfs,
        WaveformType waveform,
        int durationMs,
        double rampDurationMs = DefaultRampDurationMs)
    {
        if (toneEar == maskingEar && maskingType != MaskingType.None)
        {
            throw new ArgumentException("遮蔽耳不可與測試純音耳相同（防止同側遮蔽危險）。");
        }

        int totalSamplesPerChannel = (int)Math.Round((durationMs / 1000.0) * SampleRate);
        float[] stereoBuffer = new float[totalSamplesPerChannel * 2];

        // 1. 生成測試耳純音
        double toneAmp = DbfsToLinearAmplitude(toneDbfs);
        float[] toneMono = new float[totalSamplesPerChannel];
        if (toneAmp > 0)
        {
            if (waveform == WaveformType.Pulsed)
            {
                GeneratePulsedToneMono(toneMono, toneFrequencyHz, toneAmp, durationMs, rampDurationMs);
            }
            else
            {
                GenerateContinuousToneMono(toneMono, toneFrequencyHz, toneAmp, totalSamplesPerChannel, rampDurationMs);
            }
        }

        // 2. 生成對側耳遮蔽噪音
        float[] maskingMono = new float[totalSamplesPerChannel];
        if (maskingType != MaskingType.None)
        {
            double maskAmp = DbfsToLinearAmplitude(maskingDbfs);
            if (maskAmp > 0)
            {
                if (maskingType == MaskingType.NarrowbandNoise)
                {
                    GenerateNarrowbandNoiseMono(maskingMono, toneFrequencyHz, maskAmp, totalSamplesPerChannel, rampDurationMs);
                }
                else
                {
                    GenerateWhiteNoiseMono(maskingMono, maskAmp, totalSamplesPerChannel, rampDurationMs);
                }
            }
        }

        // 3. 聲道合成矩陣 (Channel Routing Matrix)
        for (int i = 0; i < totalSamplesPerChannel; i++)
        {
            int leftIdx = i * 2;
            int rightIdx = i * 2 + 1;

            if (toneEar == Ear.Left)
            {
                stereoBuffer[leftIdx] = toneMono[i];
                stereoBuffer[rightIdx] = maskingEar == Ear.Right ? maskingMono[i] : 0.0f;
            }
            else
            {
                stereoBuffer[leftIdx] = maskingEar == Ear.Left ? maskingMono[i] : 0.0f;
                stereoBuffer[rightIdx] = toneMono[i];
            }
        }

        return stereoBuffer;
    }

    private void GenerateContinuousToneMono(
        float[] buffer,
        int frequencyHz,
        double amplitude,
        int totalSamples,
        double rampDurationMs)
    {
        int rampSamples = (int)Math.Round((rampDurationMs / 1000.0) * SampleRate);
        rampSamples = Math.Min(rampSamples, totalSamples / 2);

        double phaseIncrement = 2.0 * Math.PI * frequencyHz / SampleRate;
        double phase = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double envelope = 1.0;
            if (i < rampSamples)
            {
                envelope = 0.5 * (1.0 - Math.Cos(Math.PI * i / rampSamples));
            }
            else if (i >= totalSamples - rampSamples)
            {
                int idx = i - (totalSamples - rampSamples);
                envelope = 0.5 * (1.0 + Math.Cos(Math.PI * idx / rampSamples));
            }

            buffer[i] = (float)(Math.Sin(phase) * amplitude * envelope);
            phase += phaseIncrement;
            if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
        }
    }

    private void GeneratePulsedToneMono(
        float[] buffer,
        int frequencyHz,
        double amplitude,
        int totalDurationMs,
        double rampDurationMs)
    {
        int pulseOnMs = 200;
        int pulseOffMs = 200;
        int cycleMs = pulseOnMs + pulseOffMs;

        int totalSamples = buffer.Length;
        int rampSamples = (int)Math.Round((rampDurationMs / 1000.0) * SampleRate);
        int pulseOnSamples = (int)Math.Round((pulseOnMs / 1000.0) * SampleRate);
        int cycleSamples = (int)Math.Round((cycleMs / 1000.0) * SampleRate);

        double phaseIncrement = 2.0 * Math.PI * frequencyHz / SampleRate;
        double phase = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            int cycleIndex = i % cycleSamples;
            double envelope = 0.0;

            if (cycleIndex < pulseOnSamples)
            {
                if (cycleIndex < rampSamples)
                {
                    envelope = 0.5 * (1.0 - Math.Cos(Math.PI * cycleIndex / rampSamples));
                }
                else if (cycleIndex >= pulseOnSamples - rampSamples)
                {
                    int idx = cycleIndex - (pulseOnSamples - rampSamples);
                    envelope = 0.5 * (1.0 + Math.Cos(Math.PI * idx / rampSamples));
                }
                else
                {
                    envelope = 1.0;
                }
            }

            buffer[i] = (float)(Math.Sin(phase) * amplitude * envelope);
            phase += phaseIncrement;
            if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
        }
    }

    /// <summary>
    /// 生成符合聽力計規格之窄頻噪音（1/3 倍頻程帶通濾波 + Raised-Cosine 平滑）
    /// </summary>
    public void GenerateNarrowbandNoiseMono(
        float[] buffer,
        int centerFrequencyHz,
        double amplitude,
        int totalSamples,
        double rampDurationMs)
    {
        // 偽隨機均勻分布白噪音 (使用固定或時間種子以確保隨機性)
        var rng = new Random();
        float[] rawNoise = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            rawNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        // 2nd Order Biquad Bandpass Filter (Q ≈ 4.318, 1/3 Octave Bandwidth)
        double q = 4.318;
        double w0 = 2.0 * Math.PI * centerFrequencyHz / SampleRate;
        double alpha = Math.Sin(w0) / (2.0 * q);

        double b0 = alpha;
        double b1 = 0.0;
        double b2 = -alpha;
        double a0 = 1.0 + alpha;
        double a1 = -2.0 * Math.Cos(w0);
        double a2 = 1.0 - alpha;

        // 歸一化係數
        double nb0 = b0 / a0;
        double nb1 = b1 / a0;
        double nb2 = b2 / a0;
        double na1 = a1 / a0;
        double na2 = a2 / a0;

        float[] filtered = new float[totalSamples];
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;

        for (int i = 0; i < totalSamples; i++)
        {
            double x0 = rawNoise[i];
            double y0 = nb0 * x0 + nb1 * x1 + nb2 * x2 - na1 * y1 - na2 * y2;

            x2 = x1;
            x1 = x0;
            y2 = y1;
            y1 = y0;

            filtered[i] = (float)y0;
        }

        // 計算濾波後訊號之均方根 (RMS)，並將 RMS 校準對齊至等效純音振幅 (amplitude / sqrt(2))
        double sumSq = 0;
        for (int i = 0; i < totalSamples; i++)
        {
            sumSq += filtered[i] * filtered[i];
        }
        double currentRms = Math.Sqrt(sumSq / totalSamples);
        double targetRms = amplitude / Math.Sqrt(2.0);
        double scale = currentRms > 1e-6 ? (targetRms / currentRms) : 0.0;

        // 套用 Raised-Cosine 漸入漸出
        int rampSamples = (int)Math.Round((rampDurationMs / 1000.0) * SampleRate);
        rampSamples = Math.Min(rampSamples, totalSamples / 2);

        for (int i = 0; i < totalSamples; i++)
        {
            double envelope = 1.0;
            if (i < rampSamples)
            {
                envelope = 0.5 * (1.0 - Math.Cos(Math.PI * i / rampSamples));
            }
            else if (i >= totalSamples - rampSamples)
            {
                int idx = i - (totalSamples - rampSamples);
                envelope = 0.5 * (1.0 + Math.Cos(Math.PI * idx / rampSamples));
            }

            buffer[i] = (float)(filtered[i] * scale * envelope);
        }
    }

    private void GenerateWhiteNoiseMono(
        float[] buffer,
        double amplitude,
        int totalSamples,
        double rampDurationMs)
    {
        var rng = new Random();
        int rampSamples = (int)Math.Round((rampDurationMs / 1000.0) * SampleRate);
        rampSamples = Math.Min(rampSamples, totalSamples / 2);

        for (int i = 0; i < totalSamples; i++)
        {
            double envelope = 1.0;
            if (i < rampSamples)
            {
                envelope = 0.5 * (1.0 - Math.Cos(Math.PI * i / rampSamples));
            }
            else if (i >= totalSamples - rampSamples)
            {
                int idx = i - (totalSamples - rampSamples);
                envelope = 0.5 * (1.0 + Math.Cos(Math.PI * idx / rampSamples));
            }

            float sample = (float)((rng.NextDouble() * 2.0 - 1.0) * amplitude * envelope);
            buffer[i] = sample;
        }
    }
}
