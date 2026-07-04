using System.Windows;
using System.Windows.Input;
using DiadiaHeicConverter.App.Services;
using DiadiaHeicConverter.App.ViewModels;

namespace DiadiaHeicConverter.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var settingsService = new SettingsService();
        var namingService = new NamingService();
        var outputPathService = new OutputPathService(namingService);
        var metadataService = new MetadataService();
        var logService = new LogService();
        var localizationService = new LocalizationService();
        var themeService = new ThemeService();
        var fileLauncherService = new FileLauncherService(logService);
        var convertEngine = new MagickImageConvertEngine(outputPathService);
        var imageConvertService = new ImageConvertService(
            metadataService,
            outputPathService,
            convertEngine,
            logService);

        DataContext = new MainViewModel(
            new FileScannerService(),
            imageConvertService,
            outputPathService,
            settingsService,
            new WindowsDialogService(),
            localizationService,
            themeService,
            fileLauncherService);
    }

    private void OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        // Mark handled so the drop is not delivered again as it bubbles to the
        // window (the empty-state zone, the add-more bar and the window all
        // subscribe), which would run the scan twice and clobber the status.
        e.Handled = true;

        if (DataContext is not MainViewModel viewModel ||
            !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await viewModel.AddPathsAsync(paths);
        }
    }
}
