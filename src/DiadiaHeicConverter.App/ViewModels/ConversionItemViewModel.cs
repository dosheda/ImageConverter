using CommunityToolkit.Mvvm.ComponentModel;
using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Resources;

namespace DiadiaHeicConverter.App.ViewModels;

public sealed class ConversionItemViewModel : ObservableObject
{
    private string _outputPath = string.Empty;
    private ConversionStatus _status = ConversionStatus.Pending;
    private string _failureReason = string.Empty;

    public ConversionItemViewModel(ConversionTaskItem model)
    {
        Model = model;
        _outputPath = model.OutputPath;
        _status = model.Status;
        _failureReason = model.FailureReason;
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

    public string SizeSummaryDisplay => Status == ConversionStatus.Failed
        ? AppStrings.Get("SizeUnreadable")
        : $"{FileSizeDisplay} · {AppStrings.Get("SizePendingSuffix")}";

    public string DetailDisplay => Status == ConversionStatus.Failed && !string.IsNullOrWhiteSpace(FailureReason)
        ? $"{SourceDirectory} · {FailureReason}"
        : SourceDirectory;

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
                OnPropertyChanged(nameof(DetailDisplay));
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
        OnPropertyChanged(nameof(SizeSummaryDisplay));
        OnPropertyChanged(nameof(DetailDisplay));
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SizeSummaryDisplay));
    }

    private static string FormatFileSize(long bytes)
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
}
