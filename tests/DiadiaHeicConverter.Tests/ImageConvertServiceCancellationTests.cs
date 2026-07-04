using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class ImageConvertServiceCancellationTests
{
    [Fact]
    public async Task ConvertAsync_marks_current_and_pending_items_cancelled()
    {
        using var temp = new TestTempDirectory();
        var firstSource = temp.Combine("first.heic");
        var secondSource = temp.Combine("second.heic");
        await File.WriteAllTextAsync(firstSource, "fake");
        await File.WriteAllTextAsync(secondSource, "fake");

        var engine = new WaitingConvertEngine();
        var service = new ImageConvertService(
            new StubMetadataService(),
            new OutputPathService(new NamingService()),
            engine,
            new NullLogService());

        var items = new List<ConversionTaskItem>
        {
            new() { SourcePath = firstSource, FileSizeBytes = 4 },
            new() { SourcePath = secondSource, FileSizeBytes = 4 }
        };
        using var cancellation = new CancellationTokenSource();

        var task = service.ConvertAsync(
            items,
            new AppSettings { OutputDirectory = temp.Combine("out") },
            progress: null,
            cancellation.Token);

        await engine.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var summary = await task;

        Assert.Equal(ConversionStatus.Cancelled, items[0].Status);
        Assert.Equal(ConversionStatus.Cancelled, items[1].Status);
        Assert.Equal(2, summary.CancelledCount);
    }

    [Fact]
    public async Task ConvertAsync_does_not_rerun_already_succeeded_items()
    {
        using var temp = new TestTempDirectory();
        var succeededSource = temp.Combine("succeeded.heic");
        var pendingSource = temp.Combine("pending.heic");
        await File.WriteAllTextAsync(succeededSource, "fake");
        await File.WriteAllTextAsync(pendingSource, "fake");

        var engine = new CountingConvertEngine();
        var service = new ImageConvertService(
            new StubMetadataService(),
            new OutputPathService(new NamingService()),
            engine,
            new NullLogService());

        var items = new List<ConversionTaskItem>
        {
            new()
            {
                SourcePath = succeededSource,
                OutputPath = temp.Combine("succeeded.jpg"),
                FileSizeBytes = 4,
                Status = ConversionStatus.Succeeded
            },
            new() { SourcePath = pendingSource, FileSizeBytes = 4 }
        };

        var summary = await service.ConvertAsync(
            items,
            new AppSettings { OutputDirectory = temp.Combine("out") },
            progress: null,
            CancellationToken.None);

        Assert.Equal(pendingSource, Assert.Single(engine.ConvertedSources));
        Assert.Equal(2, summary.SucceededCount);
    }

    [Fact]
    public async Task ConvertAsync_records_output_size_after_success()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("pending.heic");
        await File.WriteAllTextAsync(source, "fake");

        var service = new ImageConvertService(
            new StubMetadataService(),
            new OutputPathService(new NamingService()),
            new WritingConvertEngine(),
            new NullLogService());
        var item = new ConversionTaskItem
        {
            SourcePath = source,
            FileSizeBytes = 10
        };

        await service.ConvertAsync(
            [item],
            new AppSettings { OutputDirectory = temp.Combine("out") },
            progress: null,
            CancellationToken.None);

        Assert.Equal(5, item.OutputSizeBytes);
    }

    [Fact]
    public async Task ConvertAsync_preserves_source_timestamps_when_enabled()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("pending.heic");
        await File.WriteAllTextAsync(source, "fake");
        var sourceTime = new DateTime(2021, 6, 15, 8, 30, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, sourceTime);
        File.SetCreationTimeUtc(source, sourceTime);

        var service = new ImageConvertService(
            new StubMetadataService(),
            new OutputPathService(new NamingService()),
            new WritingConvertEngine(),
            new NullLogService());
        var item = new ConversionTaskItem { SourcePath = source, FileSizeBytes = 10 };

        await service.ConvertAsync(
            [item],
            new AppSettings { OutputDirectory = temp.Combine("out"), PreserveFileTimestamps = true },
            progress: null,
            CancellationToken.None);

        Assert.Equal(sourceTime, File.GetLastWriteTimeUtc(item.OutputPath));
        Assert.Equal(sourceTime, File.GetCreationTimeUtc(item.OutputPath));
    }

    [Fact]
    public async Task ConvertAsync_keeps_fresh_timestamps_when_preserve_disabled()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("pending.heic");
        await File.WriteAllTextAsync(source, "fake");
        var sourceTime = new DateTime(2021, 6, 15, 8, 30, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, sourceTime);

        var service = new ImageConvertService(
            new StubMetadataService(),
            new OutputPathService(new NamingService()),
            new WritingConvertEngine(),
            new NullLogService());
        var item = new ConversionTaskItem { SourcePath = source, FileSizeBytes = 10 };

        await service.ConvertAsync(
            [item],
            new AppSettings { OutputDirectory = temp.Combine("out"), PreserveFileTimestamps = false },
            progress: null,
            CancellationToken.None);

        Assert.NotEqual(sourceTime, File.GetLastWriteTimeUtc(item.OutputPath));
    }

    private sealed class WaitingConvertEngine : IImageConvertEngine
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string EngineName => "waiting-test-engine";

        public async Task<ConversionResult> ConvertAsync(
            ConversionTaskItem item,
            AppSettings settings,
            PhotoMetadata metadata,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return ConversionResult.Success(item.OutputPath);
        }
    }

    private sealed class CountingConvertEngine : IImageConvertEngine
    {
        public List<string> ConvertedSources { get; } = [];

        public string EngineName => "counting-test-engine";

        public Task<ConversionResult> ConvertAsync(
            ConversionTaskItem item,
            AppSettings settings,
            PhotoMetadata metadata,
            CancellationToken cancellationToken)
        {
            ConvertedSources.Add(item.SourcePath);
            return Task.FromResult(ConversionResult.Success(item.OutputPath));
        }
    }

    private sealed class WritingConvertEngine : IImageConvertEngine
    {
        public string EngineName => "writing-test-engine";

        public async Task<ConversionResult> ConvertAsync(
            ConversionTaskItem item,
            AppSettings settings,
            PhotoMetadata metadata,
            CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(item.OutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(item.OutputPath, "12345", cancellationToken);
            return ConversionResult.Success(item.OutputPath);
        }
    }

    private sealed class StubMetadataService : IMetadataService
    {
        public Task<PhotoMetadata> ReadAsync(string sourcePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(PhotoMetadata.FromFile(new FileInfo(sourcePath)));
        }
    }

    private sealed class NullLogService : ILogService
    {
        public string? CurrentLogPath => null;

        public void StartSession()
        {
        }

        public Task WriteAsync(string message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
