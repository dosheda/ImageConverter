using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public sealed class OutputPathService(INamingService namingService) : IOutputPathService
{
    public string CreateOutputPath(
        ConversionTaskItem item,
        PhotoMetadata metadata,
        AppSettings settings,
        ISet<string>? reservedDestinations = null)
    {
        var outputRoot = string.IsNullOrWhiteSpace(settings.OutputDirectory)
            ? AppSettings.GetDefaultOutputDirectory()
            : settings.OutputDirectory;

        var destinationDirectory = outputRoot;
        if (settings.PreserveDirectoryStructure && !string.IsNullOrWhiteSpace(item.RootDirectory))
        {
            var relativeDirectory = GetRelativeDirectory(item.RootDirectory, Path.GetDirectoryName(item.SourcePath));
            if (!string.IsNullOrWhiteSpace(relativeDirectory))
            {
                destinationDirectory = Path.Combine(outputRoot, relativeDirectory);
            }
        }

        var baseName = namingService.CreateBaseFileName(item.SourcePath, metadata, settings.NamingRule);
        var desiredPath = Path.Combine(destinationDirectory, $"{baseName}{settings.OutputFormat.ToFileExtension()}");

        if (settings.OverwriteExistingFiles && !IsSamePath(desiredPath, item.SourcePath))
        {
            reservedDestinations?.Add(desiredPath);
            return desiredPath;
        }

        var uniquePath = GetAvailablePath(desiredPath, reservedDestinations);
        reservedDestinations?.Add(uniquePath);
        return uniquePath;
    }

    public string CreateTemporaryPath(string finalPath)
    {
        var directory = Path.GetDirectoryName(finalPath);
        var fileName = Path.GetFileName(finalPath);
        return Path.Combine(directory ?? string.Empty, $".{fileName}.{Guid.NewGuid():N}.tmp");
    }

    private static string GetAvailablePath(string desiredPath, ISet<string>? reservedDestinations)
    {
        if (!File.Exists(desiredPath) && reservedDestinations?.Contains(desiredPath) != true)
        {
            return desiredPath;
        }

        var directory = Path.GetDirectoryName(desiredPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(desiredPath);
        var extension = Path.GetExtension(desiredPath);

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName}_{index}{extension}");
            if (!File.Exists(candidate) && reservedDestinations?.Contains(candidate) != true)
            {
                return candidate;
            }
        }

        throw new IOException("Unable to create a unique output file name.");
    }

    private static bool IsSamePath(string firstPath, string secondPath)
    {
        return string.Equals(
            Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRelativeDirectory(string rootDirectory, string? sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return string.Empty;
        }

        var root = EnsureTrailingSeparator(Path.GetFullPath(rootDirectory));
        var source = Path.GetFullPath(sourceDirectory);

        if (!source.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return Path.GetRelativePath(root, source);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
