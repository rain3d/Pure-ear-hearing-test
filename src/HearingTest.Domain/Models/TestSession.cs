namespace HearingTest.Domain.Models;

public class TestSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PatientId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string OperatorId { get; set; } = "DefaultOperator";
    public string ProtocolVersion { get; set; } = "1.0.0-SixFrequency";
    public SessionPurpose Purpose { get; set; } = SessionPurpose.ResearchTracking;
    public EnvironmentStatus EnvironmentStatus { get; set; } = EnvironmentStatus.Unverified;
    public string HardwareProfileId { get; set; } = string.Empty;
    public string CalibrationProfileId { get; set; } = string.Empty;
    public ReliabilityRating OverallReliability { get; set; } = ReliabilityRating.Good;
    public string Notes { get; set; } = string.Empty;

    public List<StimulusEvent> StimulusEvents { get; set; } = [];
    public List<ThresholdResult> ThresholdResults { get; set; } = [];
}
