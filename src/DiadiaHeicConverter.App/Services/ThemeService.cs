using System.Windows;

namespace DiadiaHeicConverter.App.Services;

public sealed class ThemeService : IThemeService
{
    private const string LightTheme = "Light";
    private const string DarkTheme = "Dark";

    public string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, DarkTheme, StringComparison.OrdinalIgnoreCase)
            ? DarkTheme
            : LightTheme;
    }

    public void ApplyTheme(string theme)
    {
        var normalized = NormalizeTheme(theme);
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var dictionaries = app.Resources.MergedDictionaries;
        var oldThemeDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains("Resources/Themes/", StringComparison.OrdinalIgnoreCase) == true);

        var newThemeDictionary = new ResourceDictionary
        {
            Source = new Uri($"Resources/Themes/{normalized}.xaml", UriKind.Relative)
        };

        if (oldThemeDictionary is not null)
        {
            // Insert the replacement before removing the old one to avoid a
            // momentary flash of unresolved DynamicResource brushes.
            var index = dictionaries.IndexOf(oldThemeDictionary);
            dictionaries.Insert(index, newThemeDictionary);
            dictionaries.Remove(oldThemeDictionary);
        }
        else
        {
            dictionaries.Add(newThemeDictionary);
        }
    }
}
