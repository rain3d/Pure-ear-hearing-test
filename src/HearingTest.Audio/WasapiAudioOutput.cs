using HearingTest.Domain;
using HearingTest.Domain.Interfaces;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace HearingTest.Audio;

public class WasapiAudioOutput : IAudioOutput
{
    private readonly AudioSignalGenerator _signalGenerator;
    private WasapiOut? _wasapiOut;
    private BufferedWaveProvider? _bufferedWaveProvider;
    private readonly object _lock = new();

    public bool IsPlaying { get; private set; }
    public bool IsExclusiveMode { get; private set; } = true;
    public int SampleRate => _signalGenerator.SampleRate;

    public event EventHandler<string>? ErrorOccurred;

    public WasapiAudioOutput(int sampleRate = AudioSignalGenerator.DefaultSampleRate, bool preferExclusive = true)
    {
        _signalGenerator = new AudioSignalGenerator(sampleRate);
        IsExclusiveMode = preferExclusive;
        InitializeDevice();
    }

    private void InitializeDevice()
    {
        lock (_lock)
        {
            try
            {
                var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_signalGenerator.SampleRate, 2);
                _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(5),
                    DiscardOnBufferOverflow = true
                };

                var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                // 優先嘗試 WASAPI Exclusive Mode；若失敗則降級為 Shared Mode
                if (IsExclusiveMode)
                {
                    try
                    {
                        _wasapiOut = new WasapiOut(defaultDevice, AudioClientShareMode.Exclusive, false, 50);
                    }
                    catch
                    {
                        // Exclusive 模式受阻，切換至 Shared 模式
                        IsExclusiveMode = false;
                        _wasapiOut = new WasapiOut(defaultDevice, AudioClientShareMode.Shared, false, 50);
                    }
                }
                else
                {
                    _wasapiOut = new WasapiOut(defaultDevice, AudioClientShareMode.Shared, false, 50);
                }

                _wasapiOut.Init(_bufferedWaveProvider);
                _wasapiOut.PlaybackStopped += OnPlaybackStopped;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"WASAPI 初始化失敗: {ex.Message}");
            }
        }
    }

    public async Task PlayToneAsync(
        Ear ear,
        int frequencyHz,
        double dbfs,
        WaveformType waveform,
        int durationMs,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_bufferedWaveProvider == null || _wasapiOut == null)
            {
                ErrorOccurred?.Invoke(this, "音訊裝置尚未就緒。");
                return;
            }

            _bufferedWaveProvider.ClearBuffer();

            // 生成平滑純音緩衝區
            float[] stereoSamples = _signalGenerator.GenerateStimulus(
                ear,
                frequencyHz,
                dbfs,
                waveform,
                durationMs);

            byte[] byteBuffer = new byte[stereoSamples.Length * sizeof(float)];
            Buffer.BlockCopy(stereoSamples, 0, byteBuffer, 0, byteBuffer.Length);

            _bufferedWaveProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);

            try
            {
                _wasapiOut.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                ErrorOccurred?.Invoke(this, $"播放失敗: {ex.Message}");
                return;
            }
        }

        // 等待播放長度完成
        try
        {
            await Task.Delay(durationMs, ct);
        }
        catch (OperationCanceledException)
        {
            MuteImmediate();
        }
        finally
        {
            Stop();
        }
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
        lock (_lock)
        {
            if (_bufferedWaveProvider == null || _wasapiOut == null)
            {
                ErrorOccurred?.Invoke(this, "音訊裝置尚未就緒。");
                return;
            }

            _bufferedWaveProvider.ClearBuffer();

            // 生成平滑立體聲（純音 + 對側遮蔽噪音）緩衝區
            float[] stereoSamples = _signalGenerator.GenerateStimulusWithMasking(
                toneEar,
                toneFrequencyHz,
                toneDbfs,
                maskingEar,
                maskingType,
                maskingDbfs,
                waveform,
                durationMs);

            byte[] byteBuffer = new byte[stereoSamples.Length * sizeof(float)];
            Buffer.BlockCopy(stereoSamples, 0, byteBuffer, 0, byteBuffer.Length);

            _bufferedWaveProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);

            try
            {
                _wasapiOut.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                ErrorOccurred?.Invoke(this, $"播放失敗: {ex.Message}");
                return;
            }
        }

        // 等待播放長度完成
        try
        {
            await Task.Delay(durationMs, ct);
        }
        catch (OperationCanceledException)
        {
            MuteImmediate();
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            try
            {
                _wasapiOut?.Stop();
                _bufferedWaveProvider?.ClearBuffer();
            }
            catch
            {
            }
            finally
            {
                IsPlaying = false;
            }
        }
    }

    public void MuteImmediate()
    {
        // 強制硬體靜音：清空緩衝區並立即停播
        Stop();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        IsPlaying = false;
        if (e.Exception != null)
        {
            ErrorOccurred?.Invoke(this, $"音訊播放中斷: {e.Exception.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        lock (_lock)
        {
            try
            {
                _wasapiOut?.Dispose();
            }
            catch
            {
            }
            _wasapiOut = null;
            _bufferedWaveProvider = null;
        }
    }
}
