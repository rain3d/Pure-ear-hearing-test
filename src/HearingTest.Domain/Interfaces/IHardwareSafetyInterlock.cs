namespace HearingTest.Domain.Interfaces;

public interface IHardwareSafetyInterlock
{
    bool IsEmergencyStopTripped { get; }
    void TriggerEmergencyStop(string reason);
    void ResetEmergencyStop();
    bool ValidateStimulusSafety(Ear ear, int frequencyHz, double levelHl, out string reason);
    event EventHandler<string>? EmergencyStopActivated;
}
