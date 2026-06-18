namespace DiadiaHeicConverter.App.Services;

public interface ILogService
{
    string? CurrentLogPath { get; }

    void StartSession();

    Task WriteAsync(string message, CancellationToken cancellationToken = default);
}
