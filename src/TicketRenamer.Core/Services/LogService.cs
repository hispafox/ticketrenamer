using System.Text.RegularExpressions;

namespace TicketRenamer.Core.Services;

public sealed partial class LogService : ILogService
{
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public LogService(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public async Task LogOperationAsync(string originalName, string? newName, bool success, string? errorMessage = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var target = newName ?? "--";
        var status = success ? "OK" : $"ERROR: {errorMessage ?? "Unknown"}";
        var line = $"{timestamp} | {originalName} → {target} | {status}";

        await _writeLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<HashSet<string>> LoadProcessedFilesAsync()
    {
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_logFilePath))
            return processed;

        var lines = await File.ReadAllLinesAsync(_logFilePath);
        foreach (var line in lines)
        {
            var match = LogLineRegex().Match(line);
            if (match.Success && line.Contains("| OK", StringComparison.Ordinal))
            {
                processed.Add(match.Groups[1].Value.Trim());
            }
        }

        return processed;
    }

    // Matches: "timestamp | original_name → ..."
    [GeneratedRegex(@"\|([^→|]+)→", RegexOptions.Compiled)]
    private static partial Regex LogLineRegex();
}
