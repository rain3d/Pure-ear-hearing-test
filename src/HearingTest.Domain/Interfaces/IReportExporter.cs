using HearingTest.Domain.Models;

namespace HearingTest.Domain.Interfaces;

public interface IReportExporter
{
    Task<string> ExportJsonAsync(TestSession session, Patient patient, string destinationPath, bool anonymized = false);
    Task<string> ExportCsvAsync(TestSession session, Patient patient, string destinationPath, bool anonymized = false);
}
