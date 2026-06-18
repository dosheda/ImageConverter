using System.Text.Json;
using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly string? _legacySettingsPath;

    public SettingsService()
        : this(GetDefaultSettingsPath(), GetLegacySettingsPath())
    {
    }

    public SettingsService(string settingsPath)
        : this(settingsPath, legacySettingsPath: null)
    {
    }

    public SettingsService(string settingsPath, string? legacySettingsPath)
    {
        _settingsPath = settingsPath;
        _legacySettingsPath = legacySettingsPath;
    }

    public AppSettings Load()
    {
        try
        {
            var pathToLoad = GetReadableSettingsPath();
            if (pathToLoad is null)
            {
                return new AppSettings().Normalized();
            }

            var json = File.ReadAllText(pathToLoad);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return (settings ?? new AppSettings()).Normalized();
        }
        catch
        {
            return new AppSettings().Normalized();
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = settings.Normalized();
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }

    private string? GetReadableSettingsPath()
    {
        if (File.Exists(_settingsPath))
        {
            return _settingsPath;
        }

        return !string.IsNullOrWhiteSpace(_legacySettingsPath) && File.Exists(_legacySettingsPath)
            ? _legacySettingsPath
            : null;
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Diadia Image Converter", "settings.json");
    }

    private static string GetLegacySettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Diadia HEIC Converter", "settings.json");
    }
}
