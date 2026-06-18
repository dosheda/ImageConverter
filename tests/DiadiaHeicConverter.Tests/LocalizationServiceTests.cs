using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void NormalizeLanguageCode_falls_back_to_simplified_chinese()
    {
        var service = new LocalizationService();

        Assert.Equal("zh-Hans", service.NormalizeLanguageCode("missing"));
    }

    [Fact]
    public void NormalizeLanguageCode_keeps_supported_language()
    {
        var service = new LocalizationService();

        Assert.Equal("ja", service.NormalizeLanguageCode("ja"));
    }

    [Theory]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-TW", "zh-Hant")]
    [InlineData("ja-JP", "ja")]
    [InlineData("en", "en-US")]
    public void NormalizeLanguageCode_keeps_old_settings_compatible(string input, string expected)
    {
        var service = new LocalizationService();

        Assert.Equal(expected, service.NormalizeLanguageCode(input));
    }
}
