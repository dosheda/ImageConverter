namespace DiadiaHeicConverter.App.Models;

public sealed record ConversionProgress(
    ConversionTaskItem Item,
    int CompletedCount,
    int TotalCount,
    string Message);
