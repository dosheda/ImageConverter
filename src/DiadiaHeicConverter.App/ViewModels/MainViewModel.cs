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
    private CancellationTokenSource? _conversionCancellation;
    private string _outputDirectory;
    private string _selectedLanguageCode;
    private string _theme;
    private int _jpegQuality;
    private OutputFormat _selectedOutputFormat;
    private NamingRule _selectedNamingRule;
    private bool _preserveExif;
    private bool _preserveGps;
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
        IThemeService themeService)
    {
        _fileScannerService = fileScannerService;
        _imageConvertService = imageConvertService;
        _outputPathService = outputPathService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _themeService = themeService;

        var settings = settingsService.Load();
        _selectedLanguageCode = localizationService.NormalizeLanguageCode(settings.LanguageCode);
        _localizationService.ApplyLanguage(_selectedLanguageCode);
        _theme = themeService.NormalizeTheme(settings.Theme);
        _themeService.ApplyTheme(_theme);
        _outputDirectory = settings.OutputDirectory;
        _jpegQuality = settings.JpegQuality;
        _selectedOutputFormat = settings.OutputFormat;
        _selectedNamingRule = settings.NamingRule;
        _preserveExif = settings.PreserveExif;
        _preserveGps = settings.PreserveGps;
        _preserveDirectoryStructure = settings.PreserveDirectoryStructure;
        _overwriteExistingFiles = settings.OverwriteExistingFiles;

        StartConversionCommand = new AsyncRelayCommand(StartConversionAsync, CanStartConversion);
        CancelConversionCommand = new RelayCommand(CancelConversion, () => IsConverting);
        ClearListCommand = new RelayCommand(ClearList, () => !IsConverting && Items.Count > 0);
        BrowseOutputDirectoryCommand = new RelayCommand(BrowseOutputDirectory, () => !IsConverting);
        OpenOutputDirectoryCommand = new RelayCommand(OpenOutputDirectory, () => !string.IsNullOrWhiteSpace(OutputDirectory));
        ToggleThemeCommand = new RelayCommand(ToggleTheme);

        LanguageOptions = localizationService.SupportedLanguages;
        RefreshNamingRuleOptions();
        RefreshOutputFormatOptions();
        _currentMessage = AppStrings.Get("InitialMessage");
    }

    public ObservableCollection<ConversionItemViewModel> Items { get; } = [];

    public ObservableCollection<NamingRuleOption> NamingRuleOptions { get; } = [];

    public ObservableCollection<OutputFormatOption> OutputFormatOptions { get; } = [];

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    public IAsyncRelayCommand StartConversionCommand { get; }

    public IRelayCommand CancelConversionCommand { get; }

    public IRelayCommand ClearListCommand { get; }

    public IRelayCommand BrowseOutputDirectoryCommand { get; }

    public IRelayCommand OpenOutputDirectoryCommand { get; }

    public IRelayCommand ToggleThemeCommand { get; }

    public bool IsDarkTheme => string.Equals(_theme, "Dark", StringComparison.OrdinalIgnoreCase);

    public string ThemeToggleGlyph => IsDarkTheme ? "☀" : "☽";

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

    public int SucceededCount => Items.Count(item => item.Status == ConversionStatus.Succeeded);

    public int FailedCount => Items.Count(item => item.Status == ConversionStatus.Failed);

    public int SkippedCount => Items.Count(item => item.Status is ConversionStatus.Skipped or ConversionStatus.Cancelled);

    public bool HasOutputDirectory => !string.IsNullOrWhiteSpace(OutputDirectory);

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

            Items.Add(new ConversionItemViewModel(item));
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
                item.RefreshFromModel();
            }

            await _imageConvertService.ConvertAsync(
                runnableItems.Select(item => item.Model).ToList(),
                settings,
                progress,
                _conversionCancellation.Token);

            RefreshAllItems();
            var summary = CreateOverallSummary();
            CurrentMessage = AppStrings.Format(
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

    private void ToggleTheme()
    {
        _theme = IsDarkTheme ? "Light" : "Dark";
        _themeService.ApplyTheme(_theme);
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeToggleGlyph));
        PersistSettings();
    }

    private void BrowseOutputDirectory()
    {
        var selected = _dialogService.SelectFolder(OutputDirectory);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            OutputDirectory = selected;
        }
    }

    private void OpenOutputDirectory()
    {
        var directory = string.IsNullOrWhiteSpace(OutputDirectory)
            ? AppSettings.GetDefaultOutputDirectory()
            : OutputDirectory;

        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{directory}\"",
            UseShellExecute = true
        });
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

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(SucceededCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(SkippedCount));
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        StartConversionCommand.NotifyCanExecuteChanged();
        CancelConversionCommand.NotifyCanExecuteChanged();
        ClearListCommand.NotifyCanExecuteChanged();
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
            PreserveDirectoryStructure = PreserveDirectoryStructure,
            OverwriteExistingFiles = OverwriteExistingFiles,
            Theme = _theme,
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
        foreach (var item in Items)
        {
            item.RefreshLocalization();
        }

        CurrentMessage = AppStrings.Get("LanguageChangedMessage");
        RefreshCounts();
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
