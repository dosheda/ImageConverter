namespace DiadiaHeicConverter.App.Models;

public sealed class ConversionTaskItem
{
    public required string SourcePath { get; init; }

    public string OutputPath { get; set; } = string.Empty;

    public string? RootDirectory { get; init; }

    public InputImageFormat InputFormat { get; init; } = InputImageFormat.Unknown;

    public long FileSizeBytes { get; init; }

    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;

    public string FailureReason { get; set; } = string.Empty;

    public InputImageFormat GetInputFormat()
    {
        return InputFormat == InputImageFormat.Unknown
            ? InputImageFormatExtensions.FromPath(SourcePath)
            : InputFormat;
    }
}
