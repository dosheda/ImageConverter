using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface IFileScannerService
{
    Task<FileScanResult> ScanAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken);
}
