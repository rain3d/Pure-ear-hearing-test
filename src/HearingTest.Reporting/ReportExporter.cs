using System.Text;
using System.Text.Json;
using HearingTest.Domain.Interfaces;
using HearingTest.Domain.Models;

namespace HearingTest.Reporting;

public class ReportExporter : IReportExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<string> ExportJsonAsync(TestSession session, Patient patient, string destinationPath, bool anonymized = false)
    {
        var exportPatient = anonymized
            ? new Patient
            {
                Id = session.PatientId,
                ExternalId = "ANONYMIZED_RESEARCH_SUBJECT",
                Name = "ANONYMOUS",
                BirthDate = null,
                Sex = patient.Sex,
                Notes = "[REDACTED_FOR_ANONYMITY]",
                ConsentAt = patient.ConsentAt
            }
            : patient;

        var payload = new
        {
            ExportedAt = DateTime.UtcNow,
            IsAnonymized = anonymized,
            Notice = "研究/個人追蹤資料，非醫療診斷報告 (Research Tracking Data - Non-Diagnostic)",
            Patient = exportPatient,
            Session = session,
            AudiogramSummary = new
            {
                LeftEar = session.ThresholdResults.Where(r => r.Ear == Domain.Ear.Left).Select(r => new { r.FrequencyHz, Threshold = r.DisplayValue }),
                RightEar = session.ThresholdResults.Where(r => r.Ear == Domain.Ear.Right).Select(r => new { r.FrequencyHz, Threshold = r.DisplayValue })
            }
        };

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(destinationPath, json, Encoding.UTF8);
        return destinationPath;
    }

    public async Task<string> ExportCsvAsync(TestSession session, Patient patient, string destinationPath, bool anonymized = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Pure-Tone Audiometry Test Export");
        sb.AppendLine($"# Date: {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"# Protocol: {session.ProtocolVersion}");
        sb.AppendLine($"# Reliability: {session.OverallReliability}");
        sb.AppendLine($"# Status: 非醫療診斷用途 (Non-Diagnostic)");

        if (!anonymized)
        {
            sb.AppendLine($"# Patient: {patient.Name} (ID: {patient.ExternalId})");
        }
        else
        {
            sb.AppendLine("# Patient: ANONYMOUS RESEARCH DATA (PII REDACTED)");
        }
        sb.AppendLine();

        // Threshold Table
        sb.AppendLine("Section,Ear,FrequencyHz,ThresholdHl,NoResponse,Masked,Reliability,AcceptedBy");
        foreach (var th in session.ThresholdResults)
        {
            sb.AppendLine($"Threshold,{th.Ear},{th.FrequencyHz},{th.ThresholdHl?.ToString("0") ?? "NR"},{th.NoResponse},{th.Masked},{th.Reliability},{th.AcceptedBy}");
        }
        sb.AppendLine();

        // Stimulus Sequence Table
        sb.AppendLine("Section,Seq,Ear,FrequencyHz,ToneLevelHl,DurationMs,MaskingEar,MaskingLevel,Response,LatencyMs,CatchTrial");
        foreach (var ev in session.StimulusEvents)
        {
            sb.AppendLine($"Stimulus,{ev.SequenceNo},{ev.Ear},{ev.FrequencyHz},{ev.ToneLevelHl?.ToString("0") ?? "Silent"},{ev.DurationMs},{ev.MaskingEar?.ToString() ?? "None"},{ev.MaskingLevel},{ev.Response},{ev.ResponseLatencyMs?.ToString() ?? ""},{ev.CatchTrial}");
        }

        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(destinationPath, sb.ToString(), Encoding.UTF8);
        return destinationPath;
    }
}
