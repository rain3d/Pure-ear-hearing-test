using HearingTest.Domain;
using HearingTest.Domain.Models;
using HearingTest.Reporting;
using Xunit;

namespace HearingTest.Tests;

public class ReportingTests
{
    [Fact]
    public async Task Test_ExportJson_AnonymizedStripsPII()
    {
        var exporter = new ReportExporter();
        var patient = new Patient
        {
            Name = "機密姓名",
            ExternalId = "ID-999888",
            BirthDate = new DateTime(1980, 1, 1),
            Notes = "非常敏感的病歷備註"
        };

        var session = new TestSession
        {
            PatientId = patient.Id,
            StartedAt = DateTime.UtcNow
        };

        string tempJson = Path.Combine(Path.GetTempPath(), $"export_anon_{Guid.NewGuid():N}.json");
        try
        {
            await exporter.ExportJsonAsync(session, patient, tempJson, anonymized: true);
            string content = await File.ReadAllTextAsync(tempJson);

            Assert.DoesNotContain("機密姓名", content);
            Assert.DoesNotContain("ID-999888", content);
            Assert.DoesNotContain("非常敏感的病歷備註", content);
            Assert.Contains("ANONYMOUS", content);
            Assert.Contains("非醫療診斷報告", content);
        }
        finally
        {
            if (File.Exists(tempJson)) File.Delete(tempJson);
        }
    }

    [Fact]
    public async Task Test_ExportCsv_StructureValid()
    {
        var exporter = new ReportExporter();
        var patient = Patient.CreateAnonymous();
        var session = new TestSession { PatientId = patient.Id };

        session.ThresholdResults.Add(new ThresholdResult
        {
            Ear = Ear.Left,
            FrequencyHz = 1000,
            ThresholdHl = 25.0,
            AcceptedBy = "Tester"
        });

        session.StimulusEvents.Add(new StimulusEvent
        {
            SequenceNo = 1,
            Ear = Ear.Left,
            FrequencyHz = 1000,
            ToneLevelHl = 25.0,
            Response = true
        });

        string tempCsv = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}.csv");
        try
        {
            await exporter.ExportCsvAsync(session, patient, tempCsv, anonymized: false);
            string content = await File.ReadAllTextAsync(tempCsv);

            Assert.Contains("Threshold,Left,1000,25", content);
            Assert.Contains("Stimulus,1,Left,1000,25", content);
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
        }
    }

    [Fact]
    public async Task Test_ExportCsv_IncludesMaskingData()
    {
        var exporter = new ReportExporter();
        var patient = Patient.CreateAnonymous();
        var session = new TestSession { PatientId = patient.Id };

        session.StimulusEvents.Add(new StimulusEvent
        {
            SequenceNo = 1,
            Ear = Ear.Left,
            FrequencyHz = 1000,
            ToneLevelHl = 45.0,
            MaskingEar = Ear.Right,
            MaskingType = MaskingType.NarrowbandNoise,
            MaskingLevel = 35.0,
            Response = true
        });

        string tempCsv = Path.Combine(Path.GetTempPath(), $"export_mask_{Guid.NewGuid():N}.csv");
        try
        {
            await exporter.ExportCsvAsync(session, patient, tempCsv);
            string content = await File.ReadAllTextAsync(tempCsv);

            Assert.Contains("Stimulus,1,Left,1000,45,3000,Right,35,True", content);
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
        }
    }
}
