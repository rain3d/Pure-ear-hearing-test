using HearingTest.Domain;
using HearingTest.Domain.Models;
using HearingTest.Persistence;
using Xunit;

namespace HearingTest.Tests;

public class PersistenceTests
{
    [Fact]
    public async Task Test_Sqlite_SaveAndLoad_Patient_Session_Events_Thresholds()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid():N}.db");
        try
        {
            using var repo = new SqliteSessionRepository(tempDb);

            var patient = new Patient
            {
                Name = "王小明",
                ExternalId = "P-001",
                BirthDate = new DateTime(1985, 5, 20),
                Sex = "Male",
                Notes = "常態檢查"
            };
            await repo.SavePatientAsync(patient);

            var loadedPatient = await repo.GetPatientAsync(patient.Id);
            Assert.NotNull(loadedPatient);
            Assert.Equal("王小明", loadedPatient.Name);

            var session = new TestSession
            {
                PatientId = patient.Id,
                StartedAt = DateTime.UtcNow,
                OperatorId = "AudiologistA",
                OverallReliability = ReliabilityRating.Good
            };

            session.StimulusEvents.Add(new StimulusEvent
            {
                SessionId = session.Id,
                SequenceNo = 1,
                Ear = Ear.Left,
                FrequencyHz = 1000,
                ToneLevelHl = 30,
                Response = true,
                ResponseLatencyMs = 350
            });

            session.ThresholdResults.Add(new ThresholdResult
            {
                SessionId = session.Id,
                Ear = Ear.Left,
                FrequencyHz = 1000,
                ThresholdHl = 25,
                AcceptedBy = "AudiologistA"
            });

            await repo.SaveSessionAsync(session);

            var loadedSession = await repo.GetSessionAsync(session.Id);
            Assert.NotNull(loadedSession);
            Assert.Single(loadedSession.StimulusEvents);
            Assert.Equal(1000, loadedSession.StimulusEvents[0].FrequencyHz);
            Assert.Single(loadedSession.ThresholdResults);
            Assert.Equal(25.0, loadedSession.ThresholdResults[0].ThresholdHl);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                File.Delete(tempDb);
            }
        }
    }

    [Fact]
    public async Task Test_Sqlite_AuditLog_Recorded()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"test_audit_{Guid.NewGuid():N}.db");
        try
        {
            using var repo = new SqliteSessionRepository(tempDb);

            var log = new AuditLog
            {
                Actor = "Admin",
                Action = "OverrideThreshold",
                EntityType = "ThresholdResult",
                EntityId = "L_1000Hz",
                Before = "25 dB HL",
                After = "30 dB HL"
            };

            await repo.SaveAuditLogAsync(log);

            var logs = await repo.GetAuditLogsAsync("L_1000Hz");
            Assert.Single(logs);
            Assert.Equal("OverrideThreshold", logs[0].Action);
            Assert.Equal("30 dB HL", logs[0].After);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                File.Delete(tempDb);
            }
        }
    }
}
