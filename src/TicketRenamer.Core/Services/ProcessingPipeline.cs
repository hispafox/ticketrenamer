using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;

namespace TicketRenamer.Core.Services;

public sealed class ProcessingPipeline : IProcessingPipeline
{
    private readonly IOcrService _ocrService;
    private readonly IBackupService _backupService;
    private readonly ILogService _logService;
    private readonly ProviderMatcher _providerMatcher;
    private readonly Action<string>? _log;

    public ProcessingPipeline(
        IOcrService ocrService,
        IBackupService backupService,
        ILogService logService,
        ProviderMatcher providerMatcher,
        Action<string>? log = null)
    {
        _ocrService = ocrService;
        _backupService = backupService;
        _logService = logService;
        _providerMatcher = providerMatcher;
        _log = log;
    }

    public Task<List<ProcessingResult>> ProcessAllAsync(ProcessingOptions options, CancellationToken ct = default)
        => ProcessAllAsync(options, progress: null, ct);

    public async Task<List<ProcessingResult>> ProcessAllAsync(ProcessingOptions options, IProgress<ProcessingResult>? progress, CancellationToken ct = default)
    {
        var results = new List<ProcessingResult>();

        // Step 1: Ensure directories exist
        Directory.CreateDirectory(options.InputFolder);
        Directory.CreateDirectory(options.OutputFolder);
        Directory.CreateDirectory(options.BackupFolder);

        // Step 2: Scan input folder for image files
        var imageFiles = Directory.GetFiles(options.InputFolder)
            .Where(f => ProcessingOptions.SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (imageFiles.Count == 0)
        {
            _log?.Invoke("No image files found in input folder.");
            return results;
        }

        _log?.Invoke($"Found {imageFiles.Count} image(s) in input folder.");

        // Step 3: Filter already-processed files
        var processed = await _logService.LoadProcessedFilesAsync();
        var newFiles = imageFiles
            .Where(f => !processed.Contains(Path.GetFileName(f).Trim()))
            .ToList();

        if (newFiles.Count == 0)
        {
            _log?.Invoke("All files have already been processed.");
            return results;
        }

        _log?.Invoke($"{newFiles.Count} new file(s) to process.");

        // Step 4: Backup ALL new files first (fail-fast)
        foreach (var file in newFiles)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(file);

            try
            {
                _log?.Invoke($"Backing up: {fileName}");
                await _backupService.BackupFileAsync(file, options.BackupFolder, ct);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"CRITICAL: Backup failed for {fileName}: {ex.Message}");
                var failResult = new ProcessingResult
                {
                    OriginalFileName = fileName,
                    Status = ProcessingStatus.BackupFailed,
                    ErrorMessage = ex.Message
                };
                results.Add(failResult);
                progress?.Report(failResult);
                await _logService.LogOperationAsync(fileName, null, false, $"Backup failed: {ex.Message}");
                // Fail-fast: stop processing if backup fails
                return results;
            }
        }

        // Step 5: Process each file (OCR -> parse -> rename -> move)
        foreach (var file in newFiles)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(file);
            var result = await ProcessSingleFileAsync(file, options, ct);
            results.Add(result);
            progress?.Report(result);

            if (result.Status == ProcessingStatus.Success)
            {
                await _logService.LogOperationAsync(fileName, result.NewFileName, true);
            }
            else
            {
                await _logService.LogOperationAsync(fileName, null, false, result.ErrorMessage);
            }
        }

        // Step 6: Validation
        ValidateBatch(options, results);

        return results;
    }

    private async Task<ProcessingResult> ProcessSingleFileAsync(
        string filePath, ProcessingOptions options, CancellationToken ct)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath);

        try
        {
            _log?.Invoke($"Processing: {fileName}");

            // OCR via Groq Vision
            var ocrResult = await _ocrService.ExtractReceiptDataAsync(filePath, ct);

            // Parse date
            var date = DateParser.Parse(ocrResult.Date);
            if (date is null)
            {
                _log?.Invoke($"  Date not found for {fileName}");
                return new ProcessingResult
                {
                    OriginalFileName = fileName,
                    Status = ProcessingStatus.DateNotFound,
                    ErrorMessage = $"Could not extract date. OCR returned: '{ocrResult.Date}'"
                };
            }

            // Match/normalize provider
            string provider;
            if (!string.IsNullOrWhiteSpace(ocrResult.Provider))
            {
                provider = _providerMatcher.Normalize(ocrResult.Provider);
            }
            else
            {
                _log?.Invoke($"  Provider not found for {fileName}");
                return new ProcessingResult
                {
                    OriginalFileName = fileName,
                    Status = ProcessingStatus.ProviderNotFound,
                    ErrorMessage = "Could not extract provider name"
                };
            }

            var receiptData = new ReceiptData
            {
                Provider = provider,
                Date = date.Value,
                OriginalFileName = fileName,
                Extension = extension
            };

            if (options.DryRun)
            {
                var previewName = FileNameBuilder.BuildPreview(receiptData);
                _log?.Invoke($"  [DRY-RUN] Would rename: {fileName} -> {previewName}");
                return new ProcessingResult
                {
                    OriginalFileName = fileName,
                    NewFileName = previewName,
                    Status = ProcessingStatus.Success
                };
            }

            // Build final name and move
            var newFileName = FileNameBuilder.Build(receiptData, options.OutputFolder);
            var destPath = Path.Combine(options.OutputFolder, newFileName);
            File.Move(filePath, destPath);

            _log?.Invoke($"  Renamed: {fileName} -> {newFileName}");

            return new ProcessingResult
            {
                OriginalFileName = fileName,
                NewFileName = newFileName,
                Status = ProcessingStatus.Success
            };
        }
        catch (HttpRequestException ex)
        {
            _log?.Invoke($"  OCR API error for {fileName}: {ex.Message}");
            return new ProcessingResult
            {
                OriginalFileName = fileName,
                Status = ProcessingStatus.OcrFailed,
                ErrorMessage = $"Groq API error: {ex.Message}"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log?.Invoke($"  Unexpected error for {fileName}: {ex.Message}");
            return new ProcessingResult
            {
                OriginalFileName = fileName,
                Status = ProcessingStatus.OcrFailed,
                ErrorMessage = ex.Message
            };
        }
    }

    private void ValidateBatch(ProcessingOptions options, List<ProcessingResult> results)
    {
        var successCount = results.Count(r => r.Status == ProcessingStatus.Success);
        var failCount = results.Count(r => r.Status != ProcessingStatus.Success);

        _log?.Invoke($"Validation: {successCount} succeeded, {failCount} failed.");

        if (!options.DryRun && successCount > 0)
        {
            var outputFiles = Directory.GetFiles(options.OutputFolder).Length;
            var backupFiles = Directory.GetFiles(options.BackupFolder).Length;
            _log?.Invoke($"  Output folder: {outputFiles} file(s). Backup folder: {backupFiles} file(s).");
        }
    }
}
