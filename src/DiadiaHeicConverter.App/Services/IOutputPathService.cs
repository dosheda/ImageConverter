using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface IOutputPathService
{
    string CreateOutputPath(
        ConversionTaskItem item,
        PhotoMetadata metadata,
        AppSettings settings,
        ISet<string>? reservedDestinations = null);

    string CreateTemporaryPath(string finalPath);
}
