using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData("Light", "Light")]
    [InlineData("light", "Light")]
    [InlineData("Dark", "Dark")]
    [InlineData("dark", "Dark")]
    [InlineData("System", "System")]
    [InlineData("system", "System")]
    [InlineData(null, "Light")]
    [InlineData("", "Light")]
    public void NormalizeTheme_canonicalizes_supported_values(string? input, string expected)
    {
        var service = new ThemeService();

        Assert.Equal(expected, service.NormalizeTheme(input));
    }

    [Fact]
    public void ResolveEffectiveTheme_returns_a_real_theme_for_system()
    {
        var service = new ThemeService();

        var effectiveTheme = service.ResolveEffectiveTheme("System");

        Assert.True(effectiveTheme is "Light" or "Dark");
    }
}
