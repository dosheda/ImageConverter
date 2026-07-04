namespace DiadiaHeicConverter.App.Services;

public interface IDialogService
{
    string? SelectFolder(string initialDirectory);

    IReadOnlyList<string> SelectInputPaths(string initialDirectory);
}
