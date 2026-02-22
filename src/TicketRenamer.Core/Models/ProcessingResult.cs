namespace TicketRenamer.Core.Models;

public sealed record ProcessingResult
{
    public required string OriginalFileName { get; init; }
    public string? NewFileName { get; init; }
    public required ProcessingStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.Now;
}
