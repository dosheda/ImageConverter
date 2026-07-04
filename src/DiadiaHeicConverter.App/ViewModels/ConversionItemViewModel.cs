using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Resources;
using DiadiaHeicConverter.App.Services;
using System.Windows;

namespace DiadiaHeicConverter.App.ViewModels;

public sealed class ConversionItemViewModel : ObservableObject
{
    private readonly IFileLauncherService? _fileLauncherService;
    private readonly Action<string>? _statusMessageCallback;
    private string _outputPath = string.Empty;
    private ConversionStatus _status = ConversionStatus.Pending;
    private string _failureReason = string.Empty;

    public ConversionItemViewModel(
        ConversionTaskItem model,
        IFileLauncherService? fileLauncherService = null,
        Func<ConversionItemViewModel, Task>? retryCallback = null,
        Action<string>? statusMessageCallback = null)
    {
        Model = model;
        _fileLauncherService = fileLauncherService;
        _statusMessageCallback = statusMessageCallback;
        _outputPath = model.OutputPath;
        _status = model.Status;
        _failureReason = model.FailureReason;

        OpenFileCommand = new RelayCommand(OpenFile, () => CanOpenOutputFile);
        RevealCommand = new RelayCommand(RevealOutput, () => CanOpenOutputFile);
        CopyOutputPathCommand = new RelayCommand(CopyOutputPath, () => CanOpenOutputFile);
        OpenSourceFolderCommand = new RelayCommand(OpenSourceFolder, CanOpenSourceFolder);
        RetryCommand = new AsyncRelayCommand(
            () => retryCallback?.Invoke(this) ?? Task.CompletedTask,
            () => retryCallback is not null && CanRetry);
    }

    public ConversionTaskItem Model { get; }

    public string SourcePath => Model.SourcePath;

    public string SourceFileName => Path.GetFileName(SourcePath);

    public string SourceDirectory => Path.GetDirectoryName(SourcePath) ?? string.Empty;

    public string OutputFileName => string.IsNullOrWhiteSpace(OutputPath)
        ? SourceFileName
        : Path.GetFileName(OutputPath);

    public string InputFormatLabel => Model.GetInputFormat() switch
    {
        InputImageFormat.Jpeg => "JPG",
        InputImageFormat.Png => "PNG",
        InputImageFormat.Webp => "WEBP",
        InputImageFormat.Heic => "HEIC",
        InputImageFormat.Heif => "HEIF",
        InputImageFormat.Bmp => "BMP",
        InputImageFormat.Tiff => "TIFF",
        _ => Path.GetExtension(SourcePath).TrimStart('.').ToUpperInvariant()
    };

    public string FileSizeDisplay => FormatFileSize(Model.FileSizeBytes);

    public string OutputSizeDisplay => Model.OutputSizeBytes is long outputSize
        ? FormatFileSize(outputSize)
        : string.Empty;

    public string SizeChangeDisplay => Status switch
    {
        ConversionStatus.Succeeded when Model.OutputSizeBytes is long outputSize =>
            $"{FileSizeDisplay} → {FormatFileSize(outputSize)}",
        ConversionStatus.Failed => AppStrings.Get("SizeUnreadable"),
        _ => $"{FileSizeDisplay} · {AppStrings.Get("SizePendingSuffix")}"
    };

    public string ReductionDisplay
    {
        get
        {
            if (Status != ConversionStatus.Succeeded ||
                Model.FileSizeBytes <= 0 ||
                Model.OutputSizeBytes is not long outputSize ||
                outputSize >= Model.FileSizeBytes)
            {
                return string.Empty;
            }

            var percent = (int)Math.Round((1 - (outputSize / (double)Model.FileSizeBytes)) * 100);
            return AppStrings.Format("ReductionFmt", Math.Clamp(percent, 0, 100));
        }
    }

    public string SizeSummaryDisplay => Status == ConversionStatus.Failed
        ? AppStrings.Get("SizeUnreadable")
        : $"{FileSizeDisplay} · {AppStrings.Get("SizePendingSuffix")}";

    public string DetailDisplay => Status == ConversionStatus.Failed && !string.IsNullOrWhiteSpace(FailureReason)
        ? $"{SourceDirectory} · {FailureReason}"
        : SourceDirectory;

    public bool CanOpenOutputFile => Status == ConversionStatus.Succeeded
        && !string.IsNullOrWhiteSpace(OutputPath)
        && File.Exists(OutputPath);

    public bool CanRetry => Status == ConversionStatus.Failed;

    public IRelayCommand OpenFileCommand { get; }

    public IRelayCommand RevealCommand { get; }

    public IRelayCommand CopyOutputPathCommand { get; }

    public IRelayCommand OpenSourceFolderCommand { get; }

    public IAsyncRelayCommand RetryCommand { get; }

    public string OutputPath
    {
        get => _outputPath;
        private set => SetProperty(ref _outputPath, value);
    }

    public ConversionStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(SizeSummaryDisplay));
                OnPropertyChanged(nameof(SizeChangeDisplay));
                OnPropertyChanged(nameof(ReductionDisplay));
                OnPropertyChanged(nameof(DetailDisplay));
                OnPropertyChanged(nameof(CanOpenOutputFile));
                OnPropertyChanged(nameof(CanRetry));
                RefreshCommandStates();
            }
        }
    }

    public string StatusText => AppStrings.GetStatusText(Status);

    public string FailureReason
    {
        get => _failureReason;
        private set
        {
            if (SetProperty(ref _failureReason, value))
            {
                OnPropertyChanged(nameof(DetailDisplay));
            }
        }
    }

    public void RefreshFromModel()
    {
        OutputPath = Model.OutputPath;
        Status = Model.Status;
        FailureReason = Model.FailureReason;
        OnPropertyChanged(nameof(OutputFileName));
        OnPropertyChanged(nameof(OutputSizeDisplay));
        OnPropertyChanged(nameof(SizeSummaryDisplay));
        OnPropertyChanged(nameof(SizeChangeDisplay));
        OnPropertyChanged(nameof(ReductionDisplay));
        OnPropertyChanged(nameof(DetailDisplay));
        OnPropertyChanged(nameof(CanOpenOutputFile));
        OnPropertyChanged(nameof(CanRetry));
        RefreshCommandStates();
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SizeSummaryDisplay));
        OnPropertyChanged(nameof(SizeChangeDisplay));
        OnPropertyChanged(nameof(ReductionDisplay));
    }

    public static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{value:0} {units[unitIndex]}" : $"{value:0.0} {units[unitIndex]}";
    }

    private void OpenFile()
    {
        if (!CanOpenOutputFile)
        {
            _statusMessageCallback?.Invoke(AppStrings.Get("ErrorFileNotFound"));
            return;
        }

        _fileLauncherService?.OpenFile(OutputPath);
    }

    private void RevealOutput()
    {
        if (!CanOpenOutputFile)
        {
            _statusMessageCallback?.Invoke(AppStrings.Get("ErrorFileNotFound"));
            return;
        }

        _fileLauncherService?.RevealInExplorer(OutputPath);
    }

    private void CopyOutputPath()
    {
        if (!CanOpenOutputFile)
        {
            _statusMessageCallback?.Invoke(AppStrings.Get("ErrorFileNotFound"));
            return;
        }

        Clipboard.SetText(OutputPath);
        _statusMessageCallback?.Invoke(AppStrings.Get("OutputPathCopiedMessage"));
    }

    private void OpenSourceFolder()
    {
        if (CanOpenSourceFolder())
        {
            _fileLauncherService?.RevealInExplorer(SourcePath);
            return;
        }

        _statusMessageCallback?.Invoke(AppStrings.Get("ErrorFileNotFound"));
    }

    private bool CanOpenSourceFolder()
    {
        return !string.IsNullOrWhiteSpace(SourcePath) && File.Exists(SourcePath);
    }

    private void RefreshCommandStates()
    {
        OpenFileCommand.NotifyCanExecuteChanged();
        RevealCommand.NotifyCanExecuteChanged();
        CopyOutputPathCommand.NotifyCanExecuteChanged();
        OpenSourceFolderCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
    }
}
