using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public sealed class FileScannerService : IFileScannerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".heic",
        ".heif",
        ".bmp",
        ".tif",
        ".tiff"
    };

    public Task<FileScanResult> ScanAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                var results = new List<ConversionTaskItem>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var unsupportedFileCount = 0;

                foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fullPath = Path.GetFullPath(path);

                    if (File.Exists(fullPath))
                    {
                        if (!AddFile(fullPath, rootDirectory: null, results, seen))
                        {
                            unsupportedFileCount++;
                        }

                        continue;
                    }

                    if (Directory.Exists(fullPath))
                    {
                        AddDirectory(fullPath, results, seen, cancellationToken, ref unsupportedFileCount);
                    }
                }

                var items = results
                    .OrderBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new FileScanResult(items, unsupportedFileCount);
            },
            cancellationToken);
    }

    public static bool IsSupportedImage(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path));
    }

    private static void AddDirectory(
        string directory,
        List<ConversionTaskItem> results,
        HashSet<string> seen,
        CancellationToken cancellationToken,
        ref int unsupportedFileCount)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System
        };

        foreach (var filePath in Directory.EnumerateFiles(directory, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!AddFile(filePath, directory, results, seen))
            {
                unsupportedFileCount++;
            }
        }
    }

    private static bool AddFile(
        string filePath,
        string? rootDirectory,
        List<ConversionTaskItem> results,
        HashSet<string> seen)
    {
        if (!IsSupportedImage(filePath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!seen.Add(fullPath))
        {
            return true;
        }

        var fileInfo = new FileInfo(fullPath);
        results.Add(new ConversionTaskItem
        {
            SourcePath = fullPath,
            RootDirectory = rootDirectory is null ? null : Path.GetFullPath(rootDirectory),
            InputFormat = InputImageFormatExtensions.FromPath(fullPath),
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
        });
        return true;
    }
}
