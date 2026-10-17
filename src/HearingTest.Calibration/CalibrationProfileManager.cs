namespace HearingTest.Calibration;

public class CalibrationProfileManager
{
    public string CalibrationDirectory { get; }

    public CalibrationProfileManager(string? customDir = null)
    {
        if (string.IsNullOrWhiteSpace(customDir))
        {
            CalibrationDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HearingTest",
                "Calibrations");
        }
        else
        {
            CalibrationDirectory = customDir;
        }

        Directory.CreateDirectory(CalibrationDirectory);
    }

    public IReadOnlyList<string> GetCalibrationFiles()
    {
        if (!Directory.Exists(CalibrationDirectory)) return [];
        return Directory.GetFiles(CalibrationDirectory, "*.json");
    }

    public string GetDefaultProfilePath()
    {
        return Path.Combine(CalibrationDirectory, "active_calibration.json");
    }
}
