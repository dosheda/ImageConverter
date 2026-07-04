namespace DiadiaHeicConverter.App.Services;

public interface IThemeService
{
    string NormalizeTheme(string? theme);

    void ApplyTheme(string theme);
}
