namespace DiadiaHeicConverter.App.Services;

public sealed class LogService : ILogService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _logDirectory;

    public LogService()
        : this(Path.Combine(AppContext.BaseDirectory, "logs"))
    {
    }

    public LogService(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    public string? CurrentLogPath { get; private set; }

    public void StartSession()
    {
        Directory.CreateDirectory(_logDirectory);
        CurrentLogPath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}.log");
        File.WriteAllText(CurrentLogPath, $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
    }

    public async Task WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(CurrentLogPath))
        {
            StartSession();
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(CurrentLogPath!, line, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
