namespace TicketRenamer.Core.Services;

public interface ILogService
{
    Task LogOperationAsync(string originalName, string? newName, bool success, string? errorMessage = null);
    Task<HashSet<string>> LoadProcessedFilesAsync();
}
