using System.Windows;
using Microsoft.Win32;

namespace DiadiaHeicConverter.App.Services;

public sealed class ThemeService : IThemeService
{
    private const string LightTheme = "Light";
    private const string DarkTheme = "Dark";
    private const string SystemTheme = "System";

    public string NormalizeTheme(string? theme)
    {
        if (string.Equals(theme, DarkTheme, StringComparison.OrdinalIgnoreCase))
        {
            return DarkTheme;
        }

        if (string.Equals(theme, SystemTheme, StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme;
        }

        return LightTheme;
    }

    public string ResolveEffectiveTheme(string theme)
    {
        var normalized = NormalizeTheme(theme);
        if (!string.Equals(normalized, SystemTheme, StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int appsUseLightTheme && appsUseLightTheme == 0
                ? DarkTheme
                : LightTheme;
        }
        catch
        {
            return LightTheme;
        }
    }

    public void ApplyTheme(string theme)
    {
        var normalized = ResolveEffectiveTheme(theme);
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
