using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;
using DiadiaHeicConverter.App.ViewModels;

namespace DiadiaHeicConverter.Tests;

public sealed class MainViewModelResumeTests
{
    [Fact]
    public async Task AddPathsAsync_scans_folder_and_adds_supported_images()
    {
        using var temp = new TestTempDirectory();
        var folder = temp.Combine("photos");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "a.jpg"), "fake");
        await File.WriteAllTextAsync(Path.Combine(folder, "b.png"), "fake");
        await File.WriteAllTextAsync(Path.Combine(folder, "note.txt"), "ignore");

        var viewModel = new MainViewModel(
            new FileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        await viewModel.AddPathsAsync(new[] { folder });

        Assert.Equal(2, viewModel.Items.Count);
        Assert.True(viewModel.HasItems);
    }

    [Fact]
    public async Task StartConversionCommand_only_submits_unfinished_items()
    {
        using var temp = new TestTempDirectory();
        var succeededSource = temp.Combine("done.heic");
        var cancelledSource = temp.Combine("todo.heic");
        await File.WriteAllTextAsync(succeededSource, "fake");
        await File.WriteAllTextAsync(cancelledSource, "fake");

        var convertService = new RecordingConvertService();
        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            convertService,
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = succeededSource,
            OutputPath = temp.Combine("done.jpg"),
            FileSizeBytes = 4,
            Status = ConversionStatus.Succeeded
        }));
        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = cancelledSource,
            FileSizeBytes = 4,
            Status = ConversionStatus.Cancelled
        }));

        await viewModel.StartConversionCommand.ExecuteAsync(null);

        var submitted = Assert.Single(convertService.SubmittedItems);
        Assert.Equal(cancelledSource, submitted.SourcePath);
    }

    [Fact]
    public async Task SelectedOutputFormat_refreshes_pending_output_preview()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("todo.heic");
        await File.WriteAllTextAsync(source, "fake");

        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = source,
            FileSizeBytes = 4,
            Status = ConversionStatus.Pending
        }));

        viewModel.SelectedOutputFormat = OutputFormat.Webp;

        Assert.EndsWith(".webp", Assert.Single(viewModel.Items).OutputPath);
    }

    [Fact]
    public async Task Webp_input_preview_uses_selected_webp_output_format()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("todo.webp");
        await File.WriteAllTextAsync(source, "fake");

        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                OutputFormat = OutputFormat.Jpeg,
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = source,
            InputFormat = InputImageFormat.Webp,
            FileSizeBytes = 4,
            Status = ConversionStatus.Pending
        }));

        viewModel.SelectedOutputFormat = OutputFormat.Webp;

        Assert.EndsWith(".webp", Assert.Single(viewModel.Items).OutputPath);
    }

    [Fact]
    public void OutputFormatOptions_include_common_output_formats()
    {
        using var temp = new TestTempDirectory();
        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        var formats = viewModel.OutputFormatOptions.Select(option => option.Value).ToList();

        Assert.Equal(
            [OutputFormat.Jpeg, OutputFormat.Png, OutputFormat.Webp, OutputFormat.Bmp, OutputFormat.Tiff],
            formats);
    }

    [Fact]
    public void SelectedTheme_persists_system_theme()
    {
        using var temp = new TestTempDirectory();
        var settingsService = new TestSettingsService(new AppSettings
        {
            OutputDirectory = temp.Combine("out"),
            LanguageCode = "zh-Hans"
        });
        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            settingsService,
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        viewModel.SelectedTheme = "System";

        Assert.True(viewModel.IsSystem);
        Assert.Equal("System", settingsService.SavedSettings?.Theme);
    }

    [Fact]
    public void Aggregate_stats_include_saved_bytes_and_completion_ratio()
    {
        using var temp = new TestTempDirectory();
        var viewModel = new MainViewModel(
            new EmptyFileScannerService(),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = temp.Combine("done.heic"),
            FileSizeBytes = 1000,
            OutputSizeBytes = 400,
            Status = ConversionStatus.Succeeded
        }));
        viewModel.Items.Add(new ConversionItemViewModel(new ConversionTaskItem
        {
            SourcePath = temp.Combine("failed.heic"),
            FileSizeBytes = 1000,
            Status = ConversionStatus.Failed
        }));

        Assert.Equal(600, viewModel.SavedBytes);
        Assert.Equal("600 B", viewModel.SavedDisplay);
        Assert.Equal("−60%", viewModel.AverageReductionDisplay);
        Assert.Equal(1, viewModel.CompletedCount);
        Assert.Equal(1, viewModel.CompletionRatio);
    }

    [Fact]
    public async Task AddPathsAsync_reports_ignored_unsupported_files()
    {
        using var temp = new TestTempDirectory();
        var viewModel = new MainViewModel(
            new StaticFileScannerService(new FileScanResult([], 3)),
            new RecordingConvertService(),
            new OutputPathService(new NamingService()),
            new TestSettingsService(new AppSettings
            {
                OutputDirectory = temp.Combine("out"),
                LanguageCode = "zh-Hans"
            }),
            new NullDialogService(),
            new LocalizationService(),
            new ThemeService(),
            new NullFileLauncherService());

        await viewModel.AddPathsAsync([temp.Path]);

        Assert.Contains("3", viewModel.CurrentMessage);
        Assert.Contains("忽略", viewModel.CurrentMessage);
    }

    private sealed class RecordingConvertService : IImageConvertService
    {
        public List<ConversionTaskItem> SubmittedItems { get; } = [];

        public Task<BatchConversionSummary> ConvertAsync(
            IReadOnlyList<ConversionTaskItem> items,
            AppSettings settings,
            IProgress<ConversionProgress>? progress,
            CancellationToken cancellationToken)
        {
            SubmittedItems.AddRange(items);
            foreach (var item in items)
            {
                item.Status = ConversionStatus.Succeeded;
            }

            return Task.FromResult(new BatchConversionSummary(items.Count, items.Count, 0, 0, 0));
        }
    }

    private sealed class EmptyFileScannerService : IFileScannerService
    {
        public Task<FileScanResult> ScanAsync(
            IEnumerable<string> paths,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileScanResult([], 0));
        }
    }

    private sealed class StaticFileScannerService(FileScanResult result) : IFileScannerService
    {
        public Task<FileScanResult> ScanAsync(
            IEnumerable<string> paths,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class TestSettingsService(AppSettings settings) : ISettingsService
    {
        public AppSettings? SavedSettings { get; private set; }

        public AppSettings Load() => settings;

        public void Save(AppSettings newSettings)
        {
            SavedSettings = newSettings;
        }
    }

    private sealed class NullDialogService : IDialogService
    {
        public string? SelectFolder(string initialDirectory) => null;

        public IReadOnlyList<string> SelectInputPaths(string initialDirectory) => [];
    }

    private sealed class NullFileLauncherService : IFileLauncherService
    {
        public void OpenFile(string path)
        {
        }

        public void RevealInExplorer(string path)
        {
        }

        public void OpenFolder(string path)
        {
        }
    }
}
