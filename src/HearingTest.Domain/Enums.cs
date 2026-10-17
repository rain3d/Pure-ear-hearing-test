namespace HearingTest.Domain;

public enum Ear
{
    Left,
    Right
}

public enum WaveformType
{
    Continuous,
    Pulsed
}

public enum MaskingType
{
    None,
    NarrowbandNoise,
    WhiteNoise
}

public enum ConductionType
{
    Air,
    Bone
}

public enum TransducerType
{
    Circumaural,
    SupraAural,
    Insert
}

public enum SessionPurpose
{
    ResearchTracking,
    ClinicalDiagnostic
}

public enum EnvironmentStatus
{
    Pass,
    Fail,
    Unverified
}

public enum ReliabilityRating
{
    Good,
    Fair,
    Poor,
    Questionable
}

public enum TestProgressState
{
    Idle,
    Familiarization,
    Testing,
    Paused,
    Completed,
    Aborted
}
