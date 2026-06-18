using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface IImageConvertService
{
    Task<BatchConversionSummary> ConvertAsync(
        IReadOnlyList<ConversionTaskItem> items,
        AppSettings settings,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken);
}
