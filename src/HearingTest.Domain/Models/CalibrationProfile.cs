namespace HearingTest.Domain.Models;

public class CalibrationPoint
{
    public int FrequencyHz { get; set; }
    public Ear Ear { get; set; }
    public double ReferenceDbfs { get; set; }
    public double MeasuredSpl { get; set; }
    public double Retspl { get; set; }
    public double ToleranceDb { get; set; } = 2.5;
}

public class CalibrationProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string HardwareProfileId { get; set; } = string.Empty;
    public string HardwareFingerprint { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime CalibratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);
    public string Laboratory { get; set; } = "Research Simulation Lab";
    public string Traceability { get; set; } = "NIST Traceable Coupler Check";
    public List<CalibrationPoint> CalibrationPoints { get; set; } = [];
    public string Signature { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
