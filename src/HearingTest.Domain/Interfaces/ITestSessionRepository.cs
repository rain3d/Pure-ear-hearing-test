using HearingTest.Domain.Models;

namespace HearingTest.Domain.Interfaces;

public interface ITestSessionRepository
{
    Task SavePatientAsync(Patient patient);
    Task<Patient?> GetPatientAsync(string id);
    Task<IReadOnlyList<Patient>> GetAllPatientsAsync();

    Task SaveSessionAsync(TestSession session);
    Task<TestSession?> GetSessionAsync(string id);
    Task<IReadOnlyList<TestSession>> GetSessionsForPatientAsync(string patientId);
    Task<IReadOnlyList<TestSession>> GetAllSessionsAsync();

    Task SaveAuditLogAsync(AuditLog log);
    Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(string? entityId = null);
}
