namespace HearingTest.Domain.Models;

public class ThresholdResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public Ear Ear { get; set; }
    public TransducerType Transducer { get; set; } = TransducerType.Circumaural;
    public ConductionType Conduction { get; set; } = ConductionType.Air;
    public int FrequencyHz { get; set; }
    public double? ThresholdHl { get; set; }
    public bool Masked { get; set; }
    public bool NoResponse { get; set; }
    public ReliabilityRating Reliability { get; set; } = ReliabilityRating.Good;
    public string AcceptedBy { get; set; } = "AutoStateMachine";
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public string DisplayValue => NoResponse 
        ? $"NR @ {ThresholdHl ?? 50:0} dB HL" 
        : $"{ThresholdHl:0} dB HL";
}
