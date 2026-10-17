using HearingTest.Calibration;
using HearingTest.Domain;
using HearingTest.Domain.Models;
using Xunit;

namespace HearingTest.Tests;

public class CalibrationTests
{
    [Fact]
    public void Test_ExpiredProfile_Blocked()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var expiredProfile = service.CreateSampleProfile(hw);
        expiredProfile.ExpiresAt = DateTime.UtcNow.AddDays(-1); // 已過期
        CalibrationService.SignProfile(expiredProfile); // 重新簽章使其本身完整，但過期

        bool valid = service.ValidateProfile(expiredProfile, hw, out string reason);
        Assert.False(valid);
        Assert.Contains("到期", reason);
    }

    [Fact]
    public void Test_HardwareFingerprintMismatch_Blocked()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile
        {
            HeadphoneModel = "OriginalHeadphones",
            HeadphoneSerialLeft = "L-ORIG",
            HeadphoneSerialRight = "R-ORIG"
        };
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var profile = service.CreateSampleProfile(hw);

        // 模擬硬體耳機更換了
        var modifiedHw = new HardwareProfile
        {
            Id = hw.Id,
            HeadphoneModel = "ReplacedHeadphones",
            HeadphoneSerialLeft = "L-NEW",
            HeadphoneSerialRight = "R-NEW"
        };
        modifiedHw.Fingerprint = HardwareFingerprint.Generate(modifiedHw);

        bool valid = service.ValidateProfile(profile, modifiedHw, out string reason);
        Assert.False(valid);
        Assert.Contains("硬體指紋不符合", reason);
    }

    [Fact]
    public void Test_AntiTamper_TamperedFile_Rejected()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var profile = service.CreateSampleProfile(hw);
        Assert.True(CalibrationService.VerifyProfileIntegrity(profile));

        // 模擬惡意人為手動竄改檔案中的實測 SPL 數值（例如從 77.0 改成 90.0）
        profile.CalibrationPoints[0].MeasuredSpl += 13.0;

        // 驗證竄改被自動抓出
        bool verified = CalibrationService.VerifyProfileIntegrity(profile);
        Assert.False(verified);

        bool valid = service.ValidateProfile(profile, hw, out string reason);
        Assert.False(valid);
        Assert.Contains("防竄改校驗失敗", reason);
    }

    [Fact]
    public async Task Test_SaveAndLoad_RoundTrip_ValidatesSuccessfully()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var profile = service.CreateSampleProfile(hw);
        string tempFile = Path.Combine(Path.GetTempPath(), $"calib_{Guid.NewGuid():N}.json");

        try
        {
            await service.SaveProfileToFileAsync(profile, tempFile);
            Assert.True(File.Exists(tempFile));

            var (loadedProfile, success, reason) = await service.LoadProfileFromFileAsync(tempFile, hw);
            Assert.True(success, $"Load failed: {reason}");
            Assert.NotNull(loadedProfile);
            Assert.True(service.IsCalibrated);
            Assert.Equal(profile.Id, loadedProfile.Id);
            Assert.Equal(12, loadedProfile.CalibrationPoints.Count); // 6 frequencies * 2 ears
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_HlToDbfs_ResearchMode_LinearFallback()
    {
        var service = new CalibrationService();
        service.SetProfile(null); // 未載入校正檔

        Assert.False(service.IsCalibrated);

        // 未校正時強制為相對研究刻度: 0 dB HL -> -70 dBFS, 50 dB HL -> -20 dBFS
        double dbfsAt0 = service.HlToDbfs(0.0, 1000, Ear.Left);
        double dbfsAt50 = service.HlToDbfs(50.0, 1000, Ear.Left);

        Assert.Equal(-70.0, dbfsAt0);
        Assert.Equal(-20.0, dbfsAt50);
    }

    [Fact]
    public void Test_HlToDbfs_Calibrated_UsesFormula()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var profile = service.CreateSampleProfile(hw);
        service.SetProfile(profile);
        Assert.True(service.IsCalibrated);

        // 以 1000 Hz 左耳為例:
        // ReferenceDbfs = -20.0, RETSPL = 7.0, MeasuredSpl = 77.0
        // Target_SPL = 40 dB HL + 7.0 = 47.0 dB SPL
        // Gain = 47.0 - 77.0 = -30.0 dB
        // Output_dBFS = -20.0 + (-30.0) = -50.0 dBFS
        double dbfs = service.HlToDbfs(40.0, 1000, Ear.Left);
        Assert.Equal(-50.0, dbfs, precision: 1);

        // 逆向測試 DbfsToHl
        double hl = service.DbfsToHl(dbfs, 1000, Ear.Left);
        Assert.Equal(40.0, hl, precision: 1);
    }

    [Fact]
    public void Test_CreatePhoneCalibrationProfile_GeneratesValidSignedProfile()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        var phoneProfile = service.CreatePhoneCalibrationProfile(hw, 68.5, 69.2, -20.0);

        Assert.NotNull(phoneProfile);
        Assert.Equal("1.0.0-PhoneSLM", phoneProfile.Version);
        Assert.Contains("手機分貝計", phoneProfile.Laboratory);
        Assert.Equal(12, phoneProfile.CalibrationPoints.Count);
        Assert.True(CalibrationService.VerifyProfileIntegrity(phoneProfile));

        bool valid = service.ValidateProfile(phoneProfile, hw, out string reason);
        Assert.True(valid, reason);
    }

    [Fact]
    public void Test_PhoneCalibration_CalculatesAccurateOffsets()
    {
        var service = new CalibrationService();
        var hw = new HardwareProfile();
        hw.Fingerprint = HardwareFingerprint.Generate(hw);

        // 假設手機實測 1000 Hz 時左耳為 67.0 dB SPL (在 -20 dBFS 下)
        var phoneProfile = service.CreatePhoneCalibrationProfile(hw, 67.0, 67.0, -20.0);
        service.SetProfile(phoneProfile);
        Assert.True(service.IsCalibrated);

        // 1000 Hz RETSPL = 7.0
        // 若要產生 30 dB HL:
        // Target_SPL = 30 + 7.0 = 37.0 dB SPL
        // Gain = 37.0 - 67.0 = -30.0 dB
        // Output_dBFS = -20.0 + (-30.0) = -50.0 dBFS
        double dbfs = service.HlToDbfs(30.0, 1000, Ear.Left);
        Assert.Equal(-50.0, dbfs, precision: 1);

        double roundTripHl = service.DbfsToHl(-50.0, 1000, Ear.Left);
        Assert.Equal(30.0, roundTripHl, precision: 1);
    }
}
