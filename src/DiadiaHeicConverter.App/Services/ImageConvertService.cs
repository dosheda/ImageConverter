using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Resources;

namespace DiadiaHeicConverter.App.Services;

public sealed class ImageConvertService(
    IMetadataService metadataService,
    IOutputPathService outputPathService,
    IImageConvertEngine convertEngine,
    ILogService logService) : IImageConvertService
{
    public async Task<BatchConversionSummary> ConvertAsync(
        IReadOnlyList<ConversionTaskItem> items,
        AppSettings settings,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var normalizedSettings = settings.Normalized();
        var total = items.Count;
        var completed = 0;
        var reservedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        logService.StartSession();
        await logService.WriteAsync($"Engine: {convertEngine.EngineName}", cancellationToken);
        await logService.WriteAsync($"Output directory: {normalizedSettings.OutputDirectory}", cancellationToken);
        await logService.WriteAsync($"Output format: {normalizedSettings.OutputFormat.ToLogName()}", cancellationToken);

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item.Status is ConversionStatus.Succeeded or ConversionStatus.Skipped)
            {
                completed++;
                progress?.Report(new ConversionProgress(
                    item,
                    completed,
                    total,
                    AppStrings.Format("ProcessedMessageFormat", completed, total)));
                continue;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                MarkRemainingCancelled(items, index);
                break;
            }

            try
            {
                item.Status = ConversionStatus.Converting;
                item.FailureReason = string.Empty;
                progress?.Report(new ConversionProgress(
                    item,
                    completed,
                    total,
                    AppStrings.Format("ConvertingFileMessageFormat", Path.GetFileName(item.SourcePath))));
                await logService.WriteAsync($"START input=\"{item.SourcePath}\"", cancellationToken);

                var metadata = await metadataService.ReadAsync(item.SourcePath, cancellationToken);
                item.OutputPath = outputPathService.CreateOutputPath(
                    item,
                    metadata,
                    normalizedSettings,
                    reservedDestinations);

                var result = await convertEngine.ConvertAsync(
                    item,
                    normalizedSettings,
                    metadata,
                    cancellationToken);

                item.Status = result.Status;
                item.FailureReason = result.ErrorMessage ?? string.Empty;

                if (result.Status == ConversionStatus.Succeeded)
                {
                    await logService.WriteAsync($"SUCCESS input=\"{item.SourcePath}\" output=\"{item.OutputPath}\"", cancellationToken);
                }
                else if (result.Status == ConversionStatus.Cancelled)
                {
                    item.FailureReason = AppStrings.Get("ErrorCancelled");
                    await logService.WriteAsync($"CANCELLED input=\"{item.SourcePath}\"", CancellationToken.None);
                    MarkRemainingCancelled(items, index + 1);
                }
                else
                {
                    await logService.WriteAsync($"FAILED input=\"{item.SourcePath}\" output=\"{item.OutputPath}\" reason=\"{item.FailureReason}\"", CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                item.Status = ConversionStatus.Cancelled;
                item.FailureReason = AppStrings.Get("ErrorCancelled");
                await logService.WriteAsync($"CANCELLED input=\"{item.SourcePath}\"", CancellationToken.None);
                MarkRemainingCancelled(items, index + 1);
            }
            catch (Exception exception)
            {
                item.Status = ConversionStatus.Failed;
                item.FailureReason = UserFriendlyErrorMapper.ToMessage(exception);
                await logService.WriteAsync($"FAILED input=\"{item.SourcePath}\" output=\"{item.OutputPath}\" reason=\"{item.FailureReason}\" exception=\"{exception}\"", CancellationToken.None);
            }
            finally
            {
                completed++;
                progress?.Report(new ConversionProgress(
                    item,
                    completed,
                    total,
                    AppStrings.Format("ProcessedMessageFormat", completed, total)));
            }

            if (item.Status == ConversionStatus.Cancelled)
            {
                break;
            }
        }

        var summary = CreateSummary(items);
        await logService.WriteAsync(
            $"SUMMARY total={summary.TotalCount} success={summary.SucceededCount} failed={summary.FailedCount} skipped={summary.SkippedCount} cancelled={summary.CancelledCount}",
            CancellationToken.None);

        return summary;
    }

    private static void MarkRemainingCancelled(IReadOnlyList<ConversionTaskItem> items, int startIndex)
    {
        for (var index = startIndex; index < items.Count; index++)
        {
            if (items[index].Status == ConversionStatus.Pending)
            {
                items[index].Status = ConversionStatus.Cancelled;
                items[index].FailureReason = AppStrings.Get("ErrorCancelled");
            }
        }
    }

    private static BatchConversionSummary CreateSummary(IReadOnlyList<ConversionTaskItem> items)
    {
        return new BatchConversionSummary(
            items.Count,
            items.Count(item => item.Status == ConversionStatus.Succeeded),
            items.Count(item => item.Status == ConversionStatus.Failed),
            items.Count(item => item.Status == ConversionStatus.Skipped),
            items.Count(item => item.Status == ConversionStatus.Cancelled));
    }
}
