namespace DiadiaHeicConverter.App.Services;

public interface IThemeService
{
    string NormalizeTheme(string? theme);

    string ResolveEffectiveTheme(string theme);

    void ApplyTheme(string theme);
}
