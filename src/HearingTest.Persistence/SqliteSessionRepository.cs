using System.Text.Json;
using HearingTest.Domain;
using HearingTest.Domain.Interfaces;
using HearingTest.Domain.Models;
using Microsoft.Data.Sqlite;

namespace HearingTest.Persistence;

public class SqliteSessionRepository : ITestSessionRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public SqliteSessionRepository(string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HearingTest");
            Directory.CreateDirectory(appData);
            dbPath = Path.Combine(appData, "hearing_test.db");
        }
        else
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Patients (
                Id TEXT PRIMARY KEY,
                ExternalId TEXT,
                Name TEXT,
                BirthDate TEXT,
                Sex TEXT,
                Notes TEXT,
                ConsentAt TEXT
            );

            CREATE TABLE IF NOT EXISTS TestSessions (
                Id TEXT PRIMARY KEY,
                PatientId TEXT,
                StartedAt TEXT,
                CompletedAt TEXT,
                OperatorId TEXT,
                ProtocolVersion TEXT,
                Purpose INTEGER,
                EnvironmentStatus INTEGER,
                HardwareProfileId TEXT,
                CalibrationProfileId TEXT,
                OverallReliability INTEGER,
                Notes TEXT,
                FOREIGN KEY(PatientId) REFERENCES Patients(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS StimulusEvents (
                Id TEXT PRIMARY KEY,
                SessionId TEXT,
                SequenceNo INTEGER,
                Ear INTEGER,
                FrequencyHz INTEGER,
                ToneLevelHl REAL,
                OutputDbfs REAL,
                Waveform INTEGER,
                DurationMs INTEGER,
                MaskingEar INTEGER,
                MaskingType INTEGER,
                MaskingLevel REAL,
                MaskingProtocolVersion TEXT,
                PlayedAt TEXT,
                Response INTEGER,
                ResponseLatencyMs INTEGER,
                CatchTrial INTEGER,
                DeviceErrors TEXT,
                Valid INTEGER,
                InvalidReason TEXT,
                FOREIGN KEY(SessionId) REFERENCES TestSessions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ThresholdResults (
                Id TEXT PRIMARY KEY,
                SessionId TEXT,
                Ear INTEGER,
                Transducer INTEGER,
                Conduction INTEGER,
                FrequencyHz INTEGER,
                ThresholdHl REAL,
                Masked INTEGER,
                NoResponse INTEGER,
                Reliability INTEGER,
                AcceptedBy TEXT,
                AcceptedAt TEXT,
                Notes TEXT,
                FOREIGN KEY(SessionId) REFERENCES TestSessions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS AuditLogs (
                Id TEXT PRIMARY KEY,
                Actor TEXT,
                Action TEXT,
                EntityType TEXT,
                EntityId TEXT,
                BeforeJson TEXT,
                AfterJson TEXT,
                Timestamp TEXT
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task SavePatientAsync(Patient patient)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Patients (Id, ExternalId, Name, BirthDate, Sex, Notes, ConsentAt)
            VALUES ($id, $externalId, $name, $birthDate, $sex, $notes, $consentAt);
        ";
        cmd.Parameters.AddWithValue("$id", patient.Id);
        cmd.Parameters.AddWithValue("$externalId", patient.ExternalId);
        cmd.Parameters.AddWithValue("$name", patient.Name);
        cmd.Parameters.AddWithValue("$birthDate", patient.BirthDate?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$sex", patient.Sex);
        cmd.Parameters.AddWithValue("$notes", patient.Notes);
        cmd.Parameters.AddWithValue("$consentAt", patient.ConsentAt?.ToString("o") ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Patient?> GetPatientAsync(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Patients WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadPatient(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<Patient>> GetAllPatientsAsync()
    {
        var list = new List<Patient>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Patients ORDER BY ConsentAt DESC;";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadPatient(reader));
        }
        return list;
    }

    public async Task SaveSessionAsync(TestSession session)
    {
        using var tx = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT OR REPLACE INTO TestSessions 
            (Id, PatientId, StartedAt, CompletedAt, OperatorId, ProtocolVersion, Purpose, EnvironmentStatus, HardwareProfileId, CalibrationProfileId, OverallReliability, Notes)
            VALUES ($id, $patientId, $startedAt, $completedAt, $operatorId, $protocolVersion, $purpose, $envStatus, $hwId, $calId, $rel, $notes);
        ";
        cmd.Parameters.AddWithValue("$id", session.Id);
        cmd.Parameters.AddWithValue("$patientId", session.PatientId);
        cmd.Parameters.AddWithValue("$startedAt", session.StartedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$completedAt", session.CompletedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$operatorId", session.OperatorId);
        cmd.Parameters.AddWithValue("$protocolVersion", session.ProtocolVersion);
        cmd.Parameters.AddWithValue("$purpose", (int)session.Purpose);
        cmd.Parameters.AddWithValue("$envStatus", (int)session.EnvironmentStatus);
        cmd.Parameters.AddWithValue("$hwId", session.HardwareProfileId);
        cmd.Parameters.AddWithValue("$calId", session.CalibrationProfileId);
        cmd.Parameters.AddWithValue("$rel", (int)session.OverallReliability);
        cmd.Parameters.AddWithValue("$notes", session.Notes);
        await cmd.ExecuteNonQueryAsync();

        // Save StimulusEvents
        foreach (var ev in session.StimulusEvents)
        {
            using var evCmd = _connection.CreateCommand();
            evCmd.Transaction = tx;
            evCmd.CommandText = @"
                INSERT OR REPLACE INTO StimulusEvents
                (Id, SessionId, SequenceNo, Ear, FrequencyHz, ToneLevelHl, OutputDbfs, Waveform, DurationMs, MaskingEar, MaskingType, MaskingLevel, MaskingProtocolVersion, PlayedAt, Response, ResponseLatencyMs, CatchTrial, DeviceErrors, Valid, InvalidReason)
                VALUES ($id, $sessionId, $seq, $ear, $freq, $toneHl, $dbfs, $waveform, $dur, $mEar, $mType, $mLevel, $mProto, $playedAt, $resp, $latency, $catch, $devErr, $valid, $invalReason);
            ";
            evCmd.Parameters.AddWithValue("$id", ev.Id);
            evCmd.Parameters.AddWithValue("$sessionId", session.Id);
            evCmd.Parameters.AddWithValue("$seq", ev.SequenceNo);
            evCmd.Parameters.AddWithValue("$ear", (int)ev.Ear);
            evCmd.Parameters.AddWithValue("$freq", ev.FrequencyHz);
            evCmd.Parameters.AddWithValue("$toneHl", ev.ToneLevelHl.HasValue ? ev.ToneLevelHl.Value : DBNull.Value);
            evCmd.Parameters.AddWithValue("$dbfs", ev.OutputDbfs);
            evCmd.Parameters.AddWithValue("$waveform", (int)ev.Waveform);
            evCmd.Parameters.AddWithValue("$dur", ev.DurationMs);
            evCmd.Parameters.AddWithValue("$mEar", ev.MaskingEar.HasValue ? (int)ev.MaskingEar.Value : DBNull.Value);
            evCmd.Parameters.AddWithValue("$mType", (int)ev.MaskingType);
            evCmd.Parameters.AddWithValue("$mLevel", ev.MaskingLevel);
            evCmd.Parameters.AddWithValue("$mProto", ev.MaskingProtocolVersion);
            evCmd.Parameters.AddWithValue("$playedAt", ev.PlayedAt.ToString("o"));
            evCmd.Parameters.AddWithValue("$resp", ev.Response ? 1 : 0);
            evCmd.Parameters.AddWithValue("$latency", ev.ResponseLatencyMs.HasValue ? ev.ResponseLatencyMs.Value : DBNull.Value);
            evCmd.Parameters.AddWithValue("$catch", ev.CatchTrial ? 1 : 0);
            evCmd.Parameters.AddWithValue("$devErr", ev.DeviceErrors);
            evCmd.Parameters.AddWithValue("$valid", ev.Valid ? 1 : 0);
            evCmd.Parameters.AddWithValue("$invalReason", ev.InvalidReason);
            await evCmd.ExecuteNonQueryAsync();
        }

        // Save ThresholdResults
        foreach (var th in session.ThresholdResults)
        {
            using var thCmd = _connection.CreateCommand();
            thCmd.Transaction = tx;
            thCmd.CommandText = @"
                INSERT OR REPLACE INTO ThresholdResults
                (Id, SessionId, Ear, Transducer, Conduction, FrequencyHz, ThresholdHl, Masked, NoResponse, Reliability, AcceptedBy, AcceptedAt, Notes)
                VALUES ($id, $sessionId, $ear, $transducer, $conduction, $freq, $thHl, $masked, $noResp, $rel, $accBy, $accAt, $notes);
            ";
            thCmd.Parameters.AddWithValue("$id", th.Id);
            thCmd.Parameters.AddWithValue("$sessionId", session.Id);
            thCmd.Parameters.AddWithValue("$ear", (int)th.Ear);
            thCmd.Parameters.AddWithValue("$transducer", (int)th.Transducer);
            thCmd.Parameters.AddWithValue("$conduction", (int)th.Conduction);
            thCmd.Parameters.AddWithValue("$freq", th.FrequencyHz);
            thCmd.Parameters.AddWithValue("$thHl", th.ThresholdHl.HasValue ? th.ThresholdHl.Value : DBNull.Value);
            thCmd.Parameters.AddWithValue("$masked", th.Masked ? 1 : 0);
            thCmd.Parameters.AddWithValue("$noResp", th.NoResponse ? 1 : 0);
            thCmd.Parameters.AddWithValue("$rel", (int)th.Reliability);
            thCmd.Parameters.AddWithValue("$accBy", th.AcceptedBy);
            thCmd.Parameters.AddWithValue("$accAt", th.AcceptedAt.ToString("o"));
            thCmd.Parameters.AddWithValue("$notes", th.Notes);
            await thCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<TestSession?> GetSessionAsync(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestSessions WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var session = ReadSession(reader);
        await LoadSessionChildrenAsync(session);
        return session;
    }

    public async Task<IReadOnlyList<TestSession>> GetSessionsForPatientAsync(string patientId)
    {
        var list = new List<TestSession>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestSessions WHERE PatientId = $pid ORDER BY StartedAt DESC;";
        cmd.Parameters.AddWithValue("$pid", patientId);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadSession(reader));
        }
        foreach (var s in list)
        {
            await LoadSessionChildrenAsync(s);
        }
        return list;
    }

    public async Task<IReadOnlyList<TestSession>> GetAllSessionsAsync()
    {
        var list = new List<TestSession>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestSessions ORDER BY StartedAt DESC;";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadSession(reader));
        }
        foreach (var s in list)
        {
            await LoadSessionChildrenAsync(s);
        }
        return list;
    }

    private async Task LoadSessionChildrenAsync(TestSession session)
    {
        // Load StimulusEvents
        using var evCmd = _connection.CreateCommand();
        evCmd.CommandText = "SELECT * FROM StimulusEvents WHERE SessionId = $sid ORDER BY SequenceNo ASC;";
        evCmd.Parameters.AddWithValue("$sid", session.Id);
        using var evReader = await evCmd.ExecuteReaderAsync();
        session.StimulusEvents.Clear();
        while (await evReader.ReadAsync())
        {
            session.StimulusEvents.Add(ReadStimulusEvent(evReader));
        }

        // Load ThresholdResults
        using var thCmd = _connection.CreateCommand();
        thCmd.CommandText = "SELECT * FROM ThresholdResults WHERE SessionId = $sid ORDER BY Ear, FrequencyHz;";
        thCmd.Parameters.AddWithValue("$sid", session.Id);
        using var thReader = await thCmd.ExecuteReaderAsync();
        session.ThresholdResults.Clear();
        while (await thReader.ReadAsync())
        {
            session.ThresholdResults.Add(ReadThresholdResult(thReader));
        }
    }

    public async Task SaveAuditLogAsync(AuditLog log)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO AuditLogs (Id, Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, Timestamp)
            VALUES ($id, $actor, $action, $type, $entityId, $before, $after, $ts);
        ";
        cmd.Parameters.AddWithValue("$id", log.Id);
        cmd.Parameters.AddWithValue("$actor", log.Actor);
        cmd.Parameters.AddWithValue("$action", log.Action);
        cmd.Parameters.AddWithValue("$type", log.EntityType);
        cmd.Parameters.AddWithValue("$entityId", log.EntityId);
        cmd.Parameters.AddWithValue("$before", log.Before);
        cmd.Parameters.AddWithValue("$after", log.After);
        cmd.Parameters.AddWithValue("$ts", log.Timestamp.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(string? entityId = null)
    {
        var list = new List<AuditLog>();
        using var cmd = _connection.CreateCommand();
        if (string.IsNullOrEmpty(entityId))
        {
            cmd.CommandText = "SELECT * FROM AuditLogs ORDER BY Timestamp DESC;";
        }
        else
        {
            cmd.CommandText = "SELECT * FROM AuditLogs WHERE EntityId = $eid ORDER BY Timestamp DESC;";
            cmd.Parameters.AddWithValue("$eid", entityId);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new AuditLog
            {
                Id = reader.GetString(0),
                Actor = reader.GetString(1),
                Action = reader.GetString(2),
                EntityType = reader.GetString(3),
                EntityId = reader.GetString(4),
                Before = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                After = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Timestamp = DateTime.Parse(reader.GetString(7))
            });
        }
        return list;
    }

    private static Patient ReadPatient(SqliteDataReader reader)
    {
        return new Patient
        {
            Id = reader.GetString(0),
            ExternalId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            BirthDate = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            Sex = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Notes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            ConsentAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6))
        };
    }

    private static TestSession ReadSession(SqliteDataReader reader)
    {
        return new TestSession
        {
            Id = reader.GetString(0),
            PatientId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            StartedAt = DateTime.Parse(reader.GetString(2)),
            CompletedAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            OperatorId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            ProtocolVersion = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Purpose = (SessionPurpose)reader.GetInt32(6),
            EnvironmentStatus = (EnvironmentStatus)reader.GetInt32(7),
            HardwareProfileId = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            CalibrationProfileId = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            OverallReliability = (ReliabilityRating)reader.GetInt32(10),
            Notes = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
        };
    }

    private static StimulusEvent ReadStimulusEvent(SqliteDataReader reader)
    {
        return new StimulusEvent
        {
            Id = reader.GetString(0),
            SessionId = reader.GetString(1),
            SequenceNo = reader.GetInt32(2),
            Ear = (Ear)reader.GetInt32(3),
            FrequencyHz = reader.GetInt32(4),
            ToneLevelHl = reader.IsDBNull(5) ? null : reader.GetDouble(5),
            OutputDbfs = reader.GetDouble(6),
            Waveform = (WaveformType)reader.GetInt32(7),
            DurationMs = reader.GetInt32(8),
            MaskingEar = reader.IsDBNull(9) ? null : (Ear)reader.GetInt32(9),
            MaskingType = (MaskingType)reader.GetInt32(10),
            MaskingLevel = reader.GetDouble(11),
            MaskingProtocolVersion = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            PlayedAt = DateTime.Parse(reader.GetString(13)),
            Response = reader.GetInt32(14) == 1,
            ResponseLatencyMs = reader.IsDBNull(15) ? null : reader.GetInt32(15),
            CatchTrial = reader.GetInt32(16) == 1,
            DeviceErrors = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
            Valid = reader.GetInt32(18) == 1,
            InvalidReason = reader.IsDBNull(19) ? string.Empty : reader.GetString(19)
        };
    }

    private static ThresholdResult ReadThresholdResult(SqliteDataReader reader)
    {
        return new ThresholdResult
        {
            Id = reader.GetString(0),
            SessionId = reader.GetString(1),
            Ear = (Ear)reader.GetInt32(2),
            Transducer = (TransducerType)reader.GetInt32(3),
            Conduction = (ConductionType)reader.GetInt32(4),
            FrequencyHz = reader.GetInt32(5),
            ThresholdHl = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            Masked = reader.GetInt32(7) == 1,
            NoResponse = reader.GetInt32(8) == 1,
            Reliability = (ReliabilityRating)reader.GetInt32(9),
            AcceptedBy = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            AcceptedAt = DateTime.Parse(reader.GetString(11)),
            Notes = reader.IsDBNull(12) ? string.Empty : reader.GetString(12)
        };
    }

    public void Dispose()
    {
        _connection.Close();
        SqliteConnection.ClearPool(_connection);
        _connection.Dispose();
    }
}
