namespace HearingTest.Domain.Interfaces;

public interface IMaskingProtocol
{
    string Version { get; }
    bool ShouldPromptMasking(Ear testEar, int frequencyHz, double testToneHl, double? contralateralAirThreshold, double? contralateralBoneThreshold);
    double CalculateInitialMaskingLevel(Ear nonTestEar, int frequencyHz, double testToneHl);
}
