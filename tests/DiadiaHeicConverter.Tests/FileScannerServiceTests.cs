using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class FileScannerServiceTests
{
    [Fact]
    public async Task ScanAsync_recursively_finds_supported_image_files()
    {
        using var temp = new TestTempDirectory();
        var nested = temp.Combine("nested");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(temp.Combine("IMG_0001.HEIC"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0002.heif"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0003.webp"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0004.jpg"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0005.jpeg"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0006.png"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0007.bmp"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0008.tif"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "IMG_0009.tiff"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "notes.txt"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "archive.rar"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "song.mp3"), "fake");
        await File.WriteAllTextAsync(Path.Combine(nested, "movie.mp4"), "fake");

        var service = new FileScannerService();
        var result = await service.ScanAsync([temp.Path], CancellationToken.None);

        Assert.Equal(9, result.Items.Count);
        Assert.Equal(4, result.UnsupportedFileCount);
        Assert.All(result.Items, item => Assert.True(FileScannerService.IsSupportedImage(item.SourcePath)));
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Jpeg);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Png);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Webp);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Heic);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Heif);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Bmp);
        Assert.Contains(result.Items, item => item.GetInputFormat() == InputImageFormat.Tiff);
        Assert.Contains(result.Items, item => item.RootDirectory == temp.Path);
    }

    [Fact]
    public async Task ScanAsync_ignores_duplicate_input_paths()
    {
        using var temp = new TestTempDirectory();
        var file = temp.Combine("IMG_0001.heic");
        await File.WriteAllTextAsync(file, "fake");

        var service = new FileScannerService();
        var result = await service.ScanAsync([file, file], CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(0, result.UnsupportedFileCount);
    }
}
