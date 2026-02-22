using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Services;

public sealed class FolderWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly IProcessingPipeline _pipeline;
    private readonly ProcessingOptions _options;
    private readonly Action<string>? _log;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public FolderWatcher(
        IProcessingPipeline pipeline,
        ProcessingOptions options,
        Action<string>? log = null)
    {
        _pipeline = pipeline;
        _options = options;
        _log = log;

        Directory.CreateDirectory(options.InputFolder);

        _watcher = new FileSystemWatcher(options.InputFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = false
        };

        foreach (var ext in ProcessingOptions.SupportedExtensions)
        {
            _watcher.Filters.Add($"*{ext}");
        }

        _watcher.Created += OnFileCreated;
    }

    public void Start()
    {
        _log?.Invoke($"Watching folder: {_options.InputFolder}");
        _log?.Invoke("Press Ctrl+C to stop.");
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
        _log?.Invoke("Watcher stopped.");
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Wait a moment for the file to finish being written
        await Task.Delay(1000);

        // Ensure only one batch runs at a time
        if (!await _processingLock.WaitAsync(0))
        {
            _log?.Invoke($"  Skipping {e.Name}: another batch is processing.");
            return;
        }

        try
        {
            _log?.Invoke($"New file detected: {e.Name}");
            var results = await _pipeline.ProcessAllAsync(_options);

            var successCount = results.Count(r => r.Status == ProcessingStatus.Success);
            var failCount = results.Count(r => r.Status != ProcessingStatus.Success);
            _log?.Invoke($"  Batch result: {successCount} succeeded, {failCount} failed.");
        }
        catch (Exception ex)
        {
            _log?.Invoke($"  Error processing batch: {ex.Message}");
        }
        finally
        {
            _processingLock.Release();
        }
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _processingLock.Dispose();
    }
}
