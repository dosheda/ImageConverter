using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;
using DiadiaHeicConverter.App.ViewModels;

namespace DiadiaHeicConverter.Tests;

public sealed class ConversionItemViewModelTests
{
    [Fact]
    public async Task Succeeded_item_shows_size_change_and_reduction()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("source.heic");
        var output = temp.Combine("source.jpg");
        await File.WriteAllTextAsync(source, "source");
        await File.WriteAllTextAsync(output, "output");

        var viewModel = new ConversionItemViewModel(
            new ConversionTaskItem
            {
                SourcePath = source,
                OutputPath = output,
                FileSizeBytes = 1000,
                OutputSizeBytes = 370,
                Status = ConversionStatus.Succeeded
            },
            new RecordingFileLauncherService());

        Assert.Equal("1000 B → 370 B", viewModel.SizeChangeDisplay);
        Assert.Equal("−63%", viewModel.ReductionDisplay);
    }

    [Fact]
    public async Task OpenFileCommand_is_only_available_for_existing_successful_output()
    {
        using var temp = new TestTempDirectory();
        var source = temp.Combine("source.heic");
        var output = temp.Combine("source.jpg");
        await File.WriteAllTextAsync(source, "source");
        await File.WriteAllTextAsync(output, "output");

        var pending = new ConversionItemViewModel(
            new ConversionTaskItem
            {
                SourcePath = source,
                OutputPath = output,
                FileSizeBytes = 1000,
                Status = ConversionStatus.Pending
            },
            new RecordingFileLauncherService());
        var succeeded = new ConversionItemViewModel(
            new ConversionTaskItem
            {
                SourcePath = source,
                OutputPath = output,
                FileSizeBytes = 1000,
                Status = ConversionStatus.Succeeded
            },
            new RecordingFileLauncherService());

        Assert.False(pending.OpenFileCommand.CanExecute(null));
        Assert.True(succeeded.OpenFileCommand.CanExecute(null));
    }

    private sealed class RecordingFileLauncherService : IFileLauncherService
    {
        public void OpenFile(string path)
        {
        }

        public void RevealInExplorer(string path)
        {
        }

        public void OpenFolder(string path)
        {
        }
    }
}
