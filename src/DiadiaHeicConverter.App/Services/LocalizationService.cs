using System.Windows;
using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Resources;

namespace DiadiaHeicConverter.App.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string DefaultLanguageCode = "zh-Hans";

    private static readonly IReadOnlyDictionary<string, string> LanguageAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] = "zh-Hans",
            ["zh-SG"] = "zh-Hans",
            ["zh-TW"] = "zh-Hant",
            ["zh-HK"] = "zh-Hant",
            ["zh-MO"] = "zh-Hant",
            ["ja-JP"] = "ja",
            ["en"] = "en-US",
            ["pt"] = "pt-BR"
        };

    public IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("zh-Hans", "简体中文"),
        new("zh-Hant", "繁體中文"),
        new("en-US", "English"),
        new("cs", "Čeština"),
        new("de", "Deutsch"),
        new("es", "Español"),
        new("fr", "Français"),
        new("it", "Italiano"),
        new("ja", "日本語"),
        new("ko", "한국어"),
        new("pl", "Polski"),
        new("pt-BR", "Português (Brasil)"),
        new("ru", "Русский"),
        new("tr", "Türkçe")
    ];

    public string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return DefaultLanguageCode;
        }

        var normalized = LanguageAliases.TryGetValue(languageCode, out var alias)
            ? alias
            : languageCode;

        return SupportedLanguages.Any(language => language.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : DefaultLanguageCode;
    }

    public void ApplyLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var dictionaries = app.Resources.MergedDictionaries;
        var oldLanguageDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains("Resources/Languages/Strings.", StringComparison.OrdinalIgnoreCase) == true);

        if (oldLanguageDictionary is not null)
        {
            dictionaries.Remove(oldLanguageDictionary);
        }

        dictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri($"Resources/Languages/Strings.{normalized}.xaml", UriKind.Relative)
        });
    }

    public string GetString(string key)
    {
        return AppStrings.Get(key);
    }
}
