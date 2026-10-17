namespace HearingTest.Domain.Models;

public class HardwareProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Computer { get; set; } = Environment.MachineName;
    public string AudioDevice { get; set; } = "DefaultAudioDevice";
    public string Driver { get; set; } = "WASAPI Exclusive";
    public string EndpointId { get; set; } = string.Empty;
    public string HeadphoneModel { get; set; } = "Generic Research Headphones";
    public string HeadphoneSerialLeft { get; set; } = "L-DEFAULT";
    public string HeadphoneSerialRight { get; set; } = "R-DEFAULT";
    public int SampleRate { get; set; } = 48000;
    public int BitDepth { get; set; } = 24;
    public float OsVolume { get; set; } = 1.0f;
    public string Fingerprint { get; set; } = string.Empty;
}
