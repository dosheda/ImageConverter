using Microsoft.Win32;
using DiadiaHeicConverter.App.Resources;

namespace DiadiaHeicConverter.App.Services;

public sealed class WindowsDialogService : IDialogService
{
    public string? SelectFolder(string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = AppStrings.Get("DialogSelectOutputTitle"),
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }

    public IReadOnlyList<string> SelectInputPaths(string initialDirectory)
    {
        var dialog = new OpenFileDialog
        {
            Title = AppStrings.Get("DialogSelectInputTitle"),
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
            Multiselect = true,
            Filter = AppStrings.Get("ImageFilesFilter")
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : [];
    }
}
