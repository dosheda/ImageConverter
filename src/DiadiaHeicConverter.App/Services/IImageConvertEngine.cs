using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface IImageConvertEngine
{
    string EngineName { get; }

    Task<ConversionResult> ConvertAsync(
        ConversionTaskItem item,
        AppSettings settings,
        PhotoMetadata metadata,
        CancellationToken cancellationToken);
}
