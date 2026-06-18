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
}
