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

    public string FileSizeDisplay => FormatFileSize(Model.FileSizeBytes);

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
            }
        }
    }

    public string StatusText => AppStrings.GetStatusText(Status);

    public string FailureReason
    {
        get => _failureReason;
        private set => SetProperty(ref _failureReason, value);
    }

    public void RefreshFromModel()
    {
        OutputPath = Model.OutputPath;
        Status = Model.Status;
        FailureReason = Model.FailureReason;
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(StatusText));
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
