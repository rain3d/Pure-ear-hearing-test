namespace HearingTest.Domain.Interfaces;

public interface IAudioOutput : IDisposable
{
    bool IsPlaying { get; }
    
    Task PlayToneAsync(
        Ear ear,
        int frequencyHz,
        double dbfs,
        WaveformType waveform,
        int durationMs,
        CancellationToken ct = default);

    Task PlayToneWithMaskingAsync(
        Ear toneEar,
        int toneFrequencyHz,
        double toneDbfs,
        Ear maskingEar,
        MaskingType maskingType,
        double maskingDbfs,
        WaveformType waveform,
        int durationMs,
        CancellationToken ct = default);

    void Stop();
    void MuteImmediate();

    event EventHandler<string>? ErrorOccurred;
}
