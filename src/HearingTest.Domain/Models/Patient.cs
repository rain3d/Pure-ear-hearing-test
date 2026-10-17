namespace HearingTest.Domain.Models;

public class Patient
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ConsentAt { get; set; }

    /// <summary>
    /// 是否為匿名快速受測
    /// </summary>
    public bool IsAnonymous => string.IsNullOrWhiteSpace(Name) || Name.StartsWith("ANON-");

    public static Patient CreateAnonymous()
    {
        var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return new Patient
        {
            ExternalId = $"ANON-{code}",
            Name = $"匿名受測者-{code}",
            ConsentAt = DateTime.UtcNow
        };
    }
}
