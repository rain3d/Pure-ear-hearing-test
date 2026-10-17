namespace HearingTest.Domain.Models;

public class StimulusEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public Ear Ear { get; set; }
    public int FrequencyHz { get; set; }
    public double? ToneLevelHl { get; set; }
    public double OutputDbfs { get; set; }
    public WaveformType Waveform { get; set; } = WaveformType.Pulsed;
    public int DurationMs { get; set; } = 3000;

    public Ear? MaskingEar { get; set; }
    public MaskingType MaskingType { get; set; } = MaskingType.None;
    public double MaskingLevel { get; set; } = 0.0;
    public string MaskingProtocolVersion { get; set; } = "1.0.0-Manual";

    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public bool Response { get; set; }
    public int? ResponseLatencyMs { get; set; }
    public bool CatchTrial { get; set; }

    public string DeviceErrors { get; set; } = string.Empty;
    public bool Valid { get; set; } = true;
    public string InvalidReason { get; set; } = string.Empty;
}
