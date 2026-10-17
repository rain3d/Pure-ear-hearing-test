using HearingTest.Domain.Models;

namespace HearingTest.Domain.Interfaces;

public interface ICalibrationService
{
    bool IsCalibrated { get; }
    CalibrationProfile? CurrentProfile { get; }

    bool ValidateProfile(CalibrationProfile profile, HardwareProfile currentHardware, out string reason);
    double HlToDbfs(double hl, int frequencyHz, Ear ear);
    double DbfsToHl(double dbfs, int frequencyHz, Ear ear);
    void SetProfile(CalibrationProfile? profile);

    Task<(CalibrationProfile? Profile, bool Success, string Reason)> LoadProfileFromFileAsync(string filePath, HardwareProfile currentHardware);
    Task SaveProfileToFileAsync(CalibrationProfile profile, string filePath);
    CalibrationProfile CreateSampleProfile(HardwareProfile hw);
    CalibrationProfile CreatePhoneCalibrationProfile(HardwareProfile hw, double measuredSplLeft, double measuredSplRight, double referenceDbfs = -20.0);
}
