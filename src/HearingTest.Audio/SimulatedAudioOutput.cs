using HearingTest.Domain;
using HearingTest.Domain.Interfaces;

namespace HearingTest.Audio;

public class SimulatedAudioOutput : IAudioOutput
{
    public bool IsPlaying { get; private set; }
    public List<StimulusPlaybackRecord> PlaybackHistory { get; } = [];
    public bool SimulatePlaybackDelay { get; set; } = false;

    public event EventHandler<string>? ErrorOccurred;

    public async Task PlayToneAsync(
        Ear ear,
        int frequencyHz,
        double dbfs,
        WaveformType waveform,
        int durationMs,
        CancellationToken ct = default)
    {
        IsPlaying = true;
        PlaybackHistory.Add(new StimulusPlaybackRecord(
            ear,
            frequencyHz,
            dbfs,
            null,
            MaskingType.None,
            0,
            waveform,
            durationMs,
            DateTime.UtcNow));

        if (SimulatePlaybackDelay && durationMs > 0)
        {
            try
            {
                await Task.Delay(durationMs, ct);
            }
            catch (OperationCanceledException)
            {
                // Stopped early
            }
        }

        IsPlaying = false;
    }

    public async Task PlayToneWithMaskingAsync(
        Ear toneEar,
        int toneFrequencyHz,
        double toneDbfs,
        Ear maskingEar,
        MaskingType maskingType,
        double maskingDbfs,
        WaveformType waveform,
        int durationMs,
        CancellationToken ct = default)
    {
        IsPlaying = true;
        PlaybackHistory.Add(new StimulusPlaybackRecord(
            toneEar,
            toneFrequencyHz,
            toneDbfs,
            maskingEar,
            maskingType,
            maskingDbfs,
            waveform,
            durationMs,
            DateTime.UtcNow));

        if (SimulatePlaybackDelay && durationMs > 0)
        {
            try
            {
                await Task.Delay(durationMs, ct);
            }
            catch (OperationCanceledException)
            {
                // Stopped early
            }
        }

        IsPlaying = false;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void MuteImmediate()
    {
        IsPlaying = false;
    }

    public void SimulateError(string message)
    {
        IsPlaying = false;
        ErrorOccurred?.Invoke(this, message);
    }

    public void Dispose()
    {
        Stop();
    }
}

public record StimulusPlaybackRecord(
    Ear ToneEar,
    int ToneFrequencyHz,
    double ToneDbfs,
    Ear? MaskingEar,
    MaskingType MaskingType,
    double MaskingDbfs,
    WaveformType Waveform,
    int DurationMs,
    DateTime Timestamp);
