using System.Diagnostics;

namespace DiadiaHeicConverter.App.Services;

public sealed class FileLauncherService(ILogService logService) : IFileLauncherService
{
    public void OpenFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                WriteLaunchFailure("OpenFile", path, "File does not exist.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            WriteLaunchFailure("OpenFile", path, exception.ToString());
        }
    }

    public void RevealInExplorer(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                WriteLaunchFailure("RevealInExplorer", path, "File does not exist.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            WriteLaunchFailure("RevealInExplorer", path, exception.ToString());
        }
    }

    public void OpenFolder(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                WriteLaunchFailure("OpenFolder", path, "Directory does not exist.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            WriteLaunchFailure("OpenFolder", path, exception.ToString());
        }
    }

    private void WriteLaunchFailure(string operation, string? path, string reason)
    {
        _ = WriteLaunchFailureAsync(operation, path, reason);
    }

    private async Task WriteLaunchFailureAsync(string operation, string? path, string reason)
    {
        try
        {
            await logService.WriteAsync(
                $"LAUNCH_FAILED operation=\"{operation}\" path=\"{path}\" reason=\"{reason}\"",
                CancellationToken.None);
        }
        catch
        {
        }
    }
}
