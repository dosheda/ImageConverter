using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface IMetadataService
{
    Task<PhotoMetadata> ReadAsync(string sourcePath, CancellationToken cancellationToken);
}
