using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class OutputPathServiceTests
{
    [Fact]
    public async Task CreateOutputPath_renames_when_jpg_exists_by_default()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.HEIC");
        var existing = temp.Combine("out", "IMG_1234.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        await File.WriteAllTextAsync(source, "fake");
        await File.WriteAllTextAsync(existing, "existing");

        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            FileSizeBytes = 4
        };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = Path.GetDirectoryName(existing)!,
            OverwriteExistingFiles = false
        });

        Assert.EndsWith("IMG_1234_1.jpg", result);
    }

    [Fact]
    public async Task CreateOutputPath_preserves_relative_directory_when_enabled()
    {
        using var temp = new TestTempDirectory();
        var root = temp.Combine("input");
        var nested = Path.Combine(root, "album", "day1");
        Directory.CreateDirectory(nested);
        var source = Path.Combine(nested, "IMG_1234.heic");
        await File.WriteAllTextAsync(source, "fake");

        var output = temp.Combine("out");
        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            RootDirectory = root,
            FileSizeBytes = 4
        };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = output,
            PreserveDirectoryStructure = true
        });

        Assert.Equal(Path.Combine(output, "album", "day1", "IMG_1234.jpg"), result);
    }

    [Fact]
    public async Task CreateOutputPath_uses_reserved_names_for_same_batch()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.heic");
        await File.WriteAllTextAsync(source, "fake");

        var service = new OutputPathService(new NamingService());
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var settings = new AppSettings { OutputDirectory = temp.Path };
        var item = new ConversionTaskItem { SourcePath = source, FileSizeBytes = 4 };

        var first = service.CreateOutputPath(item, MetadataFor(source), settings, reserved);
        var second = service.CreateOutputPath(item, MetadataFor(source), settings, reserved);

        Assert.EndsWith("IMG_1234.jpg", first);
        Assert.EndsWith("IMG_1234_1.jpg", second);
    }

    [Fact]
    public async Task CreateOutputPath_uses_webp_extension_when_selected()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.heic");
        var existing = temp.Combine("IMG_1234.webp");
        await File.WriteAllTextAsync(source, "fake");
        await File.WriteAllTextAsync(existing, "existing");

        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem { SourcePath = source, FileSizeBytes = 4 };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = temp.Path,
            OutputFormat = OutputFormat.Webp
        });

        Assert.EndsWith("IMG_1234_1.webp", result);
    }

    [Theory]
    [InlineData(OutputFormat.Jpeg, ".jpg")]
    [InlineData(OutputFormat.Png, ".png")]
    [InlineData(OutputFormat.Webp, ".webp")]
    [InlineData(OutputFormat.Bmp, ".bmp")]
    [InlineData(OutputFormat.Tiff, ".tiff")]
    public async Task CreateOutputPath_uses_selected_output_format_extension(OutputFormat outputFormat, string extension)
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.heic");
        await File.WriteAllTextAsync(source, "fake");

        var output = temp.Combine("out");
        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem { SourcePath = source, FileSizeBytes = 4 };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = output,
            OutputFormat = outputFormat
        });

        Assert.Equal(Path.Combine(output, $"IMG_1234{extension}"), result);
    }

    [Fact]
    public async Task CreateOutputPath_allows_same_format_webp_output()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.webp");
        await File.WriteAllTextAsync(source, "fake");

        var output = temp.Combine("out");
        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            InputFormat = InputImageFormat.Webp,
            FileSizeBytes = 4
        };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = output,
            OutputFormat = OutputFormat.Webp
        });

        Assert.Equal(Path.Combine(output, "IMG_1234.webp"), result);
    }

    [Fact]
    public async Task CreateOutputPath_same_format_never_returns_source_path()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("IMG_1234.webp");
        await File.WriteAllTextAsync(source, "fake");

        var service = new OutputPathService(new NamingService());
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            InputFormat = InputImageFormat.Webp,
            FileSizeBytes = 4
        };

        var result = service.CreateOutputPath(item, MetadataFor(source), new AppSettings
        {
            OutputDirectory = temp.Path,
            OutputFormat = OutputFormat.Webp,
            OverwriteExistingFiles = true
        });

        Assert.NotEqual(Path.GetFullPath(source), Path.GetFullPath(result));
        Assert.EndsWith("IMG_1234_1.webp", result);
    }

    private static PhotoMetadata MetadataFor(string path)
    {
        return PhotoMetadata.FromFile(new FileInfo(path));
    }
}
