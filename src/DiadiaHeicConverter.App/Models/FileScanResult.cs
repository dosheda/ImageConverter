namespace DiadiaHeicConverter.App.Models;

public sealed record FileScanResult(
    IReadOnlyList<ConversionTaskItem> Items,
    int UnsupportedFileCount);
