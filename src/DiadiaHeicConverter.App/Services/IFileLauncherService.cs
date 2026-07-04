namespace DiadiaHeicConverter.App.Services;

public interface IFileLauncherService
{
    void OpenFile(string path);

    void RevealInExplorer(string path);

    void OpenFolder(string path);
}
