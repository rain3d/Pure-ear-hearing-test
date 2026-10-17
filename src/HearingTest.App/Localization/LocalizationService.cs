using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace HearingTest.App.Localization;

public static class LocalizationService
{
    public const string TraditionalChinese = "zh-TW";
    public const string SimplifiedChinese = "zh-CN";
    public const string English = "en-US";

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HearingTest",
        "preferences.json");

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new LanguageOption(TraditionalChinese, "繁體中文", "繁體中文"),
        new LanguageOption(SimplifiedChinese, "简体中文", "简体中文"),
        new LanguageOption(English, "English", "English")
    ];

    public static LanguageOption CurrentLanguage { get; private set; } = SupportedLanguages[0];

    public static event Action? LanguageChanged;

    public static void Initialize()
    {
        string langCode = LoadSavedLanguageCode();
        SetLanguage(langCode, savePreference: false);
    }

    public static void SetLanguage(string languageCode, bool savePreference = true)
    {
        var matched = SupportedLanguages.FirstOrDefault(l =>
            string.Equals(l.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            ?? SupportedLanguages[0];

        CurrentLanguage = matched;

        // Apply CultureInfo
        try
        {
            var culture = new CultureInfo(matched.Code);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // Fallback gracefully
        }

        // Apply ResourceDictionary to Application
        if (Application.Current != null)
        {
            var uri = new Uri($"pack://application:,,,/Resources/Strings/Strings.{matched.Code}.xaml", UriKind.Absolute);
            var newDict = new ResourceDictionary { Source = uri };

            var merged = Application.Current.Resources.MergedDictionaries;
            var oldDict = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Strings."));

            if (oldDict != null)
            {
                int index = merged.IndexOf(oldDict);
                merged.RemoveAt(index);
                merged.Insert(index, newDict);
            }
            else
            {
                merged.Add(newDict);
            }
        }

        if (savePreference)
        {
            SaveLanguageCode(matched.Code);
        }

        LanguageChanged?.Invoke();
    }

    public static string GetString(string key, string fallback = "")
    {
        if (Application.Current != null && Application.Current.TryFindResource(key) is string val)
        {
            return val;
        }
        return fallback.Length > 0 ? fallback : key;
    }

    private static string LoadSavedLanguageCode()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Language", out var langElem))
                {
                    string? saved = langElem.GetString();
                    if (!string.IsNullOrWhiteSpace(saved) &&
                        SupportedLanguages.Any(l => string.Equals(l.Code, saved, StringComparison.OrdinalIgnoreCase)))
                    {
                        return saved;
                    }
                }
            }
        }
        catch
        {
            // Ignore load errors
        }

        // Default to Traditional Chinese
        return TraditionalChinese;
    }

    private static void SaveLanguageCode(string code)
    {
        try
        {
            string? dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dict = new Dictionary<string, string> { ["Language"] = code };
            string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Ignore write errors
        }
    }
}
