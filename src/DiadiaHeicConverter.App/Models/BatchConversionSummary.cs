namespace DiadiaHeicConverter.App.Models;

public sealed record BatchConversionSummary(
    int TotalCount,
    int SucceededCount,
    int FailedCount,
    int SkippedCount,
    int CancelledCount);
