namespace HearingTest.App.Localization;

public class LanguageOption
{
    public string Code { get; }
    public string DisplayName { get; }
    public string NativeName { get; }

    public LanguageOption(string code, string displayName, string nativeName)
    {
        Code = code;
        DisplayName = displayName;
        NativeName = nativeName;
    }

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) => obj is LanguageOption other && Code == other.Code;
    public override int GetHashCode() => Code.GetHashCode();
}
