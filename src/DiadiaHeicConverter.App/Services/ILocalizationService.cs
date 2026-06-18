using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface ILocalizationService
{
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }

    string NormalizeLanguageCode(string? languageCode);

    void ApplyLanguage(string languageCode);

    string GetString(string key);
}
