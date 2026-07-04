using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Resources;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFileScannerService _fileScannerService;
    private readonly IImageConvertService _imageConvertService;
    private readonly IOutputPathService _outputPathService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    private readonly IFileLauncherService _fileLauncherService;
    private CancellationTokenSource? _conversionCancellation;
    private string _outputDirectory;
    private string _selectedLanguageCode;
    private string _selectedTheme;
    private int _jpegQuality;
    private OutputFormat _selectedOutputFormat;
    private NamingRule _selectedNamingRule;
    private bool _preserveExif;
    private bool _preserveGps;
    private bool _preserveFileTimestamps;
    private bool _preserveDirectoryStructure;
    private bool _overwriteExistingFiles;
    private bool _isConverting;
    private double _progressValue;
    private double _progressMaximum = 1;
    private string _currentMessage = string.Empty;

    public MainViewModel(
        IFileScannerService fileScannerService,
        IImageConvertService imageConvertService,
        IOutputPathService outputPathService,
        ISettingsService settingsService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IThemeService themeService,
        IFileLauncherService fileLauncherService)
    {
        _fileScannerService = fileScannerService;
        _imageConvertService = imageConvertService;
        _outputPathService = outputPathService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _themeService = themeService;
        _fileLauncherService = fileLauncherService;

        var settings = settingsService.Load();
        _selectedLanguageCode = localizationService.NormalizeLanguageCode(settings.LanguageCode);
        _localizationService.ApplyLanguage(_selectedLanguageCode);
        _selectedTheme = themeService.NormalizeTheme(settings.Theme);
        _themeService.ApplyTheme(_selectedTheme);
        _outputDirectory = settings.OutputDirectory;
        _jpegQuality = settings.JpegQuality;
        _selectedOutputFormat = settings.OutputFormat;
        _selectedNamingRule = settings.NamingRule;
        _preserveExif = settings.PreserveExif;
        _preserveGps = settings.PreserveGps;
        _preserveFileTimestamps = settings.PreserveFileTimestamps;
        _preserveDirectoryStructure = settings.PreserveDirectoryStructure;
        _overwriteExistingFiles = settings.OverwriteExistingFiles;

        StartConversionCommand = new AsyncRelayCommand(StartConversionAsync, CanStartConversion);
        CancelConversionCommand = new RelayCommand(CancelConversion, () => IsConverting);
        ClearListCommand = new RelayCommand(ClearList, () => !IsConverting && Items.Count > 0);
        BrowseInputPathsCommand = new AsyncRelayCommand(BrowseInputPathsAsync, () => !IsConverting);
        BrowseOutputDirectoryCommand = new RelayCommand(BrowseOutputDirectory, () => !IsConverting);
        OpenOutputDirectoryCommand = new RelayCommand(OpenOutputDirectory, () => !string.IsNullOrWhiteSpace(OutputDirectory));
        OpenGitHubCommand = new RelayCommand(() => OpenExternalLink("https://github.com/dosheda/ImageConverter"));
        OpenHelpCommand = new RelayCommand(() => OpenExternalLink("https://github.com/dosheda/ImageConverter#readme"));
        OpenReleasesCommand = new RelayCommand(() => OpenExternalLink("https://github.com/dosheda/ImageConverter/releases"));
        OpenLicensesCommand = new RelayCommand(() => OpenExternalLink("https://github.com/dosheda/ImageConverter/blob/main/LICENSE"));

        LanguageOptions = localizationService.SupportedLanguages;
        RefreshThemeOptions();
        RefreshNamingRuleOptions();
        RefreshOutputFormatOptions();
        _currentMessage = AppStrings.Get("InitialMessage");
    }

    public ObservableCollection<ConversionItemViewModel> Items { get; } = [];

    public ObservableCollection<NamingRuleOption> NamingRuleOptions { get; } = [];

    public ObservableCollection<OutputFormatOption> OutputFormatOptions { get; } = [];

    public ObservableCollection<ThemeOption> ThemeOptions { get; } = [];

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    public IAsyncRelayCommand StartConversionCommand { get; }

    public IRelayCommand CancelConversionCommand { get; }

    public IRelayCommand ClearListCommand { get; }

    public IAsyncRelayCommand BrowseInputPathsCommand { get; }

    public IRelayCommand BrowseOutputDirectoryCommand { get; }

    public IRelayCommand OpenOutputDirectoryCommand { get; }

    public IRelayCommand OpenGitHubCommand { get; }

    public IRelayCommand OpenHelpCommand { get; }

    public IRelayCommand OpenReleasesCommand { get; }

    public IRelayCommand OpenLicensesCommand { get; }

    public string AppVersion
    {
        get
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            var normalized = _themeService.NormalizeTheme(value);
            if (SetProperty(ref _selectedTheme, normalized))
            {
                _themeService.ApplyTheme(normalized);
                RefreshThemeState();
                PersistSettings();
            }
        }
    }

    public bool IsLight => string.Equals(SelectedTheme, "Light", StringComparison.OrdinalIgnoreCase);

    public bool IsDark => string.Equals(SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase);

    public bool IsSystem => string.Equals(SelectedTheme, "System", StringComparison.OrdinalIgnoreCase);

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (SetProperty(ref _outputDirectory, value))
            {
                PersistSettings();
                RefreshPreviewOutputPaths();
                OnPropertyChanged(nameof(HasOutputDirectory));
                RefreshCommandStates();
            }
        }
    }

    public string SelectedLanguageCode
    {
        get => _selectedLanguageCode;
        set
        {
            var normalized = _localizationService.NormalizeLanguageCode(value);
            if (SetProperty(ref _selectedLanguageCode, normalized))
            {
                _localizationService.ApplyLanguage(normalized);
                RefreshLocalization();
                PersistSettings();
            }
        }
    }

    public int JpegQuality
    {
        get => _jpegQuality;
        set
        {
            var clamped = Math.Clamp(value, 1, 100);
            if (SetProperty(ref _jpegQuality, clamped))
            {
                PersistSettings();
            }
        }
    }

    public OutputFormat SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            if (SetProperty(ref _selectedOutputFormat, value))
            {
                PersistSettings();
                RefreshPreviewOutputPaths();
            }
        }
    }

    public NamingRule SelectedNamingRule
    {
        get => _selectedNamingRule;
        set
        {
            if (SetProperty(ref _selectedNamingRule, value))
            {
                PersistSettings();
                RefreshPreviewOutputPaths();
            }
        }
    }

    public bool PreserveExif
    {
        get => _preserveExif;
        set
        {
            if (SetProperty(ref _preserveExif, value))
            {
                PersistSettings();
            }
        }
    }

    public bool PreserveGps
    {
        get => _preserveGps;
        set
        {
            if (SetProperty(ref _preserveGps, value))
            {
                PersistSettings();
            }
        }
    }

    public bool PreserveFileTimestamps
    {
        get => _preserveFileTimestamps;
        set
        {
            if (SetProperty(ref _preserveFileTimestamps, value))
            {
                PersistSettings();
            }
        }
    }

    public bool PreserveDirectoryStructure
    {
        get => _preserveDirectoryStructure;
        set
        {
            if (SetProperty(ref _preserveDirectoryStructure, value))
            {
                PersistSettings();
                RefreshPreviewOutputPaths();
            }
        }
    }

    public bool OverwriteExistingFiles
    {
        get => _overwriteExistingFiles;
        set
        {
            if (SetProperty(ref _overwriteExistingFiles, value))
            {
                PersistSettings();
                RefreshPreviewOutputPaths();
            }
        }
    }

    public bool IsConverting
    {
        get => _isConverting;
        private set
        {
            if (SetProperty(ref _isConverting, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public double ProgressMaximum
    {
        get => _progressMaximum;
        private set => SetProperty(ref _progressMaximum, value);
    }

    public string CurrentMessage
    {
        get => _currentMessage;
        private set => SetProperty(ref _currentMessage, value);
    }

    public int TotalCount => Items.Count;

    public bool HasItems => Items.Count > 0;

    public int SucceededCount => Items.Count(item => item.Status == ConversionStatus.Succeeded);

    public int CompletedCount => SucceededCount;

    public int FailedCount => Items.Count(item => item.Status == ConversionStatus.Failed);

    public int PendingCount => Items.Count(item => item.Status == ConversionStatus.Pending);

    public int SkippedCount => Items.Count(item => item.Status is ConversionStatus.Skipped or ConversionStatus.Cancelled);

    public int RunnableCount => Items.Count(item => IsRunnableStatus(item.Status));

    public string StartButtonText => AppStrings.Format("StartButtonWithCountFmt", RunnableCount);

    public double CompletionRatio => TotalCount == 0
        ? 0
        : Math.Clamp(FinishedCount / (double)TotalCount, 0, 1);

    public string CompletionPercentDisplay => $"{CompletionRatio * 100:0}%";

    public long SavedBytes => Items
        .Where(item => item.Status == ConversionStatus.Succeeded && item.Model.OutputSizeBytes is not null)
        .Sum(item => Math.Max(0, item.Model.FileSizeBytes - item.Model.OutputSizeBytes!.Value));

    public string SavedDisplay => ConversionItemViewModel.FormatFileSize(SavedBytes);

    public double AverageReductionRatio
    {
        get
        {
            var succeededItems = Items
                .Where(item => item.Status == ConversionStatus.Succeeded && item.Model.OutputSizeBytes is not null)
                .ToList();
            var inputBytes = succeededItems.Sum(item => item.Model.FileSizeBytes);
            if (inputBytes <= 0)
            {
                return 0;
            }

            var outputBytes = succeededItems.Sum(item => item.Model.OutputSizeBytes!.Value);
            return Math.Clamp(1 - (outputBytes / (double)inputBytes), 0, 1);
        }
    }

    public double AverageReductionPercent => AverageReductionRatio * 100;

    public string AverageReductionDisplay => AverageReductionRatio <= 0
        ? string.Empty
        : AppStrings.Format(
            "ReductionFmt",
            (int)Math.Round(AverageReductionRatio * 100));

    public bool HasOutputDirectory => !string.IsNullOrWhiteSpace(OutputDirectory);

    private int FinishedCount => Items.Count(item => item.Status is
        ConversionStatus.Succeeded or
        ConversionStatus.Failed or
        ConversionStatus.Skipped or
        ConversionStatus.Cancelled);

    public async Task AddPathsAsync(IEnumerable<string> paths)
    {
        if (IsConverting)
        {
            return;
        }

        var scanResult = await _fileScannerService.ScanAsync(paths, CancellationToken.None);
        var existing = Items
            .Select(item => item.SourcePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var item in scanResult.Items)
        {
            if (!existing.Add(item.SourcePath))
            {
                continue;
            }

            Items.Add(CreateItemViewModel(item));
            added++;
        }

        RefreshPreviewOutputPaths();
        CurrentMessage = CreateAddPathsMessage(added, scanResult.UnsupportedFileCount);
        RefreshCounts();
        RefreshCommandStates();
    }

    private async Task StartConversionAsync()
    {
        if (!CanStartConversion())
        {
            return;
        }

        var runnableItems = Items
            .Where(item => IsRunnableStatus(item.Status))
            .ToList();
        if (runnableItems.Count == 0)
        {
            return;
        }

        await ConvertItemsAsync(runnableItems);
    }

    private async Task RetryItemAsync(ConversionItemViewModel item)
    {
        if (IsConverting || item.Status != ConversionStatus.Failed)
        {
            return;
        }

        CurrentMessage = AppStrings.Format("RetryingFileMessageFormat", Path.GetFileName(item.SourcePath));
        await ConvertItemsAsync([item]);
    }

    private async Task ConvertItemsAsync(IReadOnlyList<ConversionItemViewModel> runnableItems)
    {
        if (runnableItems.Count == 0)
        {
            return;
        }

        IsConverting = true;
        ProgressValue = 0;
        ProgressMaximum = Math.Max(runnableItems.Count, 1);
        _conversionCancellation = new CancellationTokenSource();

        var settings = CaptureSettings();
        var progress = new Progress<ConversionProgress>(OnConversionProgress);

        try
        {
            RefreshPreviewOutputPaths();

            foreach (var item in runnableItems)
            {
                item.Model.Status = ConversionStatus.Pending;
                item.Model.FailureReason = string.Empty;
                item.Model.OutputSizeBytes = null;
                item.RefreshFromModel();
            }

            await _imageConvertService.ConvertAsync(
                runnableItems.Select(item => item.Model).ToList(),
                settings,
                progress,
                _conversionCancellation.Token);

            RefreshAllItems();
            var summary = CreateOverallSummary();
            CurrentMessage = summary.FailedCount == 0 &&
                summary.SkippedCount == 0 &&
                summary.CancelledCount == 0 &&
                summary.SucceededCount == summary.TotalCount
                    ? $"✓ {AppStrings.Get("AllDone")}"
                    : AppStrings.Format(
                        "CompletedMessageFormat",
                        summary.TotalCount,
                        summary.SucceededCount,
                        summary.FailedCount,
                        summary.SkippedCount + summary.CancelledCount);
        }
        finally
        {
            _conversionCancellation.Dispose();
            _conversionCancellation = null;
            IsConverting = false;
            RefreshAllItems();
            RefreshCounts();
            PersistSettings();
        }
    }

    private void OnConversionProgress(ConversionProgress progress)
    {
        var itemViewModel = Items.FirstOrDefault(item => ReferenceEquals(item.Model, progress.Item));
        itemViewModel?.RefreshFromModel();
        ProgressValue = progress.CompletedCount;
        ProgressMaximum = Math.Max(progress.TotalCount, 1);
        CurrentMessage = progress.Message;
        RefreshCounts();
    }

    private void CancelConversion()
    {
        _conversionCancellation?.Cancel();
        CurrentMessage = AppStrings.Get("CancellingMessage");
    }

    private void ClearList()
    {
        Items.Clear();
        ProgressValue = 0;
        ProgressMaximum = 1;
        CurrentMessage = AppStrings.Get("ListClearedMessage");
        RefreshCounts();
        RefreshCommandStates();
    }

    private void BrowseOutputDirectory()
    {
        var selected = _dialogService.SelectFolder(OutputDirectory);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            OutputDirectory = selected;
        }
    }

    private async Task BrowseInputPathsAsync()
    {
        var selectedPaths = _dialogService.SelectInputPaths(OutputDirectory);
        if (selectedPaths.Count == 0)
        {
            return;
        }

        await AddPathsAsync(selectedPaths);
    }

    private void OpenOutputDirectory()
    {
        var directory = string.IsNullOrWhiteSpace(OutputDirectory)
            ? AppSettings.GetDefaultOutputDirectory()
            : OutputDirectory;

        Directory.CreateDirectory(directory);
        _fileLauncherService.OpenFolder(directory);
    }

    private void OpenExternalLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            CurrentMessage = AppStrings.Get("ErrorUnknown");
        }
    }

    private bool CanStartConversion()
    {
        return !IsConverting
            && Items.Count > 0
            && !string.IsNullOrWhiteSpace(OutputDirectory)
            && Items.Any(item => IsRunnableStatus(item.Status));
    }

    private void RefreshPreviewOutputPaths()
    {
        var settings = CaptureSettings();
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in Items)
        {
            if (item.Status == ConversionStatus.Succeeded)
            {
                continue;
            }

            var metadata = PhotoMetadata.FromFile(new FileInfo(item.SourcePath));
            item.Model.OutputPath = _outputPathService.CreateOutputPath(item.Model, metadata, settings, reserved);
            item.RefreshFromModel();
        }
    }

    private void RefreshAllItems()
    {
        foreach (var item in Items)
        {
            item.RefreshFromModel();
        }
    }

    private ConversionItemViewModel CreateItemViewModel(ConversionTaskItem item)
    {
        return new ConversionItemViewModel(
            item,
            _fileLauncherService,
            RetryItemAsync,
            SetCurrentMessage);
    }

    private void SetCurrentMessage(string message)
    {
        CurrentMessage = message;
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(SucceededCount));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(SkippedCount));
        OnPropertyChanged(nameof(RunnableCount));
        OnPropertyChanged(nameof(StartButtonText));
        OnPropertyChanged(nameof(CompletionRatio));
        OnPropertyChanged(nameof(CompletionPercentDisplay));
        OnPropertyChanged(nameof(SavedBytes));
        OnPropertyChanged(nameof(SavedDisplay));
        OnPropertyChanged(nameof(AverageReductionRatio));
        OnPropertyChanged(nameof(AverageReductionPercent));
        OnPropertyChanged(nameof(AverageReductionDisplay));
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        StartConversionCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        ClearListCommand.NotifyCanExecuteChanged();
        BrowseInputPathsCommand.NotifyCanExecuteChanged();
        BrowseOutputDirectoryCommand.NotifyCanExecuteChanged();
        OpenOutputDirectoryCommand.NotifyCanExecuteChanged();
    }

    private AppSettings CaptureSettings()
    {
        return new AppSettings
        {
            OutputDirectory = OutputDirectory,
            JpegQuality = JpegQuality,
            OutputFormat = SelectedOutputFormat,
            NamingRule = SelectedNamingRule,
            PreserveExif = PreserveExif,
            PreserveGps = PreserveGps,
            PreserveFileTimestamps = PreserveFileTimestamps,
            PreserveDirectoryStructure = PreserveDirectoryStructure,
            OverwriteExistingFiles = OverwriteExistingFiles,
            Theme = SelectedTheme,
            LanguageCode = SelectedLanguageCode
        }.Normalized();
    }

    private BatchConversionSummary CreateOverallSummary()
    {
        var models = Items.Select(item => item.Model).ToList();
        return new BatchConversionSummary(
            models.Count,
            models.Count(item => item.Status == ConversionStatus.Succeeded),
            models.Count(item => item.Status == ConversionStatus.Failed),
            models.Count(item => item.Status == ConversionStatus.Skipped),
            models.Count(item => item.Status == ConversionStatus.Cancelled));
    }

    private static bool IsRunnableStatus(ConversionStatus status)
    {
        return status is ConversionStatus.Pending
            or ConversionStatus.Failed
            or ConversionStatus.Cancelled
            or ConversionStatus.Converting;
    }

    private static string CreateAddPathsMessage(int added, int unsupportedFileCount)
    {
        if (added > 0 && unsupportedFileCount > 0)
        {
            return AppStrings.Format("AddedFilesWithIgnoredMessageFormat", added, unsupportedFileCount);
        }

        if (added > 0)
        {
            return AppStrings.Format("AddedFilesMessageFormat", added);
        }

        if (unsupportedFileCount > 0)
        {
            return AppStrings.Format("NoNewFilesWithIgnoredMessageFormat", unsupportedFileCount);
        }

        return AppStrings.Get("NoNewFilesMessage");
    }

    private void PersistSettings()
    {
        try
        {
            _settingsService.Save(CaptureSettings());
        }
        catch
        {
            CurrentMessage = AppStrings.Get("SettingsSaveFailedMessage");
        }
    }

    private void RefreshLocalization()
    {
        RefreshNamingRuleOptions();
        RefreshOutputFormatOptions();
        RefreshThemeOptions();
        foreach (var item in Items)
        {
            item.RefreshLocalization();
        }

        CurrentMessage = AppStrings.Get("LanguageChangedMessage");
        RefreshCounts();
    }

    private void RefreshThemeOptions()
    {
        var selectedTheme = _selectedTheme;
        ThemeOptions.Clear();
        ThemeOptions.Add(new("Light", AppStrings.Get("ThemeLight")));
        ThemeOptions.Add(new("Dark", AppStrings.Get("ThemeDark")));
        ThemeOptions.Add(new("System", AppStrings.Get("ThemeSystem")));
        _selectedTheme = selectedTheme;
        OnPropertyChanged(nameof(SelectedTheme));
    }

    private void RefreshThemeState()
    {
        OnPropertyChanged(nameof(IsLight));
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(IsSystem));
    }

    private void RefreshNamingRuleOptions()
    {
        var selectedRule = _selectedNamingRule;
        NamingRuleOptions.Clear();
        NamingRuleOptions.Add(new(NamingRule.OriginalFileName, AppStrings.Get("NamingOriginal")));
        NamingRuleOptions.Add(new(NamingRule.DateTimeOriginalAndFileName, AppStrings.Get("NamingDateTimeOriginal")));
        NamingRuleOptions.Add(new(NamingRule.DateAndOriginalFileName, AppStrings.Get("NamingDateOriginal")));
        _selectedNamingRule = selectedRule;
        OnPropertyChanged(nameof(SelectedNamingRule));
    }

    private void RefreshOutputFormatOptions()
    {
        var selectedFormat = _selectedOutputFormat;
        OutputFormatOptions.Clear();
        OutputFormatOptions.Add(new(OutputFormat.Jpeg, AppStrings.Get("FormatJpeg")));
        OutputFormatOptions.Add(new(OutputFormat.Png, AppStrings.Get("FormatPng")));
        OutputFormatOptions.Add(new(OutputFormat.Webp, AppStrings.Get("FormatWebp")));
        OutputFormatOptions.Add(new(OutputFormat.Bmp, AppStrings.Get("FormatBmp")));
        OutputFormatOptions.Add(new(OutputFormat.Tiff, AppStrings.Get("FormatTiff")));
        _selectedOutputFormat = selectedFormat;
        OnPropertyChanged(nameof(SelectedOutputFormat));
    }
}
