using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;
using ImageMagick;

namespace DiadiaHeicConverter.Tests;

public sealed class MagickImageConvertEngineTests
{
    [Theory]
    [InlineData(OutputFormat.Jpeg, MagickFormat.Jpeg, "output.jpg")]
    [InlineData(OutputFormat.Png, MagickFormat.Png, "output.png")]
    [InlineData(OutputFormat.Webp, MagickFormat.WebP, "output.webp")]
    [InlineData(OutputFormat.Bmp, MagickFormat.Bmp, "output.bmp")]
    [InlineData(OutputFormat.Tiff, MagickFormat.Tiff, "output.tiff")]
    public async Task ConvertAsync_writes_selected_output_format(
        OutputFormat outputFormat,
        MagickFormat expectedFormat,
        string outputName)
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("source.png");
        var output = temp.Combine(outputName);

        using (var image = new MagickImage(MagickColors.Red, 2, 2))
        {
            image.Write(source, MagickFormat.Png);
        }

        var engine = new MagickImageConvertEngine(new OutputPathService(new NamingService()));
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            OutputPath = output,
            FileSizeBytes = new FileInfo(source).Length
        };

        var result = await engine.ConvertAsync(
            item,
            new AppSettings { OutputFormat = outputFormat },
            PhotoMetadata.FromFile(new FileInfo(source)),
            CancellationToken.None);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.True(File.Exists(output));

        using var converted = new MagickImage(output);
        Assert.Equal(expectedFormat, converted.Format);
    }

    [Fact]
    public async Task ConvertAsync_allows_same_format_webp_reencode()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("source.webp");
        var output = temp.Combine("output.webp");

        using (var image = new MagickImage(MagickColors.Blue, 2, 2))
        {
            image.Write(source, MagickFormat.WebP);
        }

        var engine = new MagickImageConvertEngine(new OutputPathService(new NamingService()));
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            OutputPath = output,
            InputFormat = InputImageFormat.Webp,
            FileSizeBytes = new FileInfo(source).Length
        };

        var result = await engine.ConvertAsync(
            item,
            new AppSettings { OutputFormat = OutputFormat.Webp },
            PhotoMetadata.FromFile(new FileInfo(source)),
            CancellationToken.None);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.True(File.Exists(output));

        using var converted = new MagickImage(output);
        Assert.Equal(MagickFormat.WebP, converted.Format);
    }
}
