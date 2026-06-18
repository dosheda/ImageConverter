using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Save_and_load_round_trips_settings()
    {
        using var temp = new TestTempDirectory();
        var settingsPath = temp.Combine("settings.json");
        var service = new SettingsService(settingsPath);

        service.Save(new AppSettings
        {
            OutputDirectory = temp.Combine("out"),
            JpegQuality = 88,
            OutputFormat = OutputFormat.Webp,
            NamingRule = NamingRule.DateAndOriginalFileName,
            PreserveExif = true,
            PreserveGps = true,
            PreserveDirectoryStructure = true,
            OverwriteExistingFiles = true,
            LanguageCode = "en-US"
        });

        var loaded = service.Load();

        Assert.Equal(88, loaded.JpegQuality);
        Assert.Equal(OutputFormat.Webp, loaded.OutputFormat);
        Assert.Equal(NamingRule.DateAndOriginalFileName, loaded.NamingRule);
        Assert.True(loaded.PreserveGps);
        Assert.True(loaded.PreserveDirectoryStructure);
        Assert.True(loaded.OverwriteExistingFiles);
        Assert.Equal("en-US", loaded.LanguageCode);
    }

    [Fact]
    public void Load_reads_legacy_settings_when_new_settings_do_not_exist()
    {
        using var temp = new TestTempDirectory();
        var settingsPath = temp.Combine("new", "settings.json");
        var legacySettingsPath = temp.Combine("old", "settings.json");
        var legacyService = new SettingsService(legacySettingsPath);
        legacyService.Save(new AppSettings
        {
            OutputDirectory = temp.Combine("legacy-out"),
            JpegQuality = 76,
            OutputFormat = OutputFormat.Png,
            LanguageCode = "en-US"
        });

        var service = new SettingsService(settingsPath, legacySettingsPath);
        var loaded = service.Load();

        Assert.Equal(76, loaded.JpegQuality);
        Assert.Equal(OutputFormat.Png, loaded.OutputFormat);
        Assert.Equal("en-US", loaded.LanguageCode);
    }
}
