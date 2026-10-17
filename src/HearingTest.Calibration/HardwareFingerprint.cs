using System.Security.Cryptography;
using System.Text;
using HearingTest.Domain.Models;

namespace HearingTest.Calibration;

public static class HardwareFingerprint
{
    public static string Generate(HardwareProfile profile)
    {
        var raw = $"{profile.Computer}|{profile.AudioDevice}|{profile.EndpointId}|" +
                  $"{profile.HeadphoneModel}|{profile.HeadphoneSerialLeft}|{profile.HeadphoneSerialRight}|" +
                  $"{profile.SampleRate}|{profile.BitDepth}";

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
