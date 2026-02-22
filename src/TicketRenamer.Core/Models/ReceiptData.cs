namespace TicketRenamer.Core.Models;

public sealed record ReceiptData
{
    public required string Provider { get; init; }
    public required DateOnly Date { get; init; }
    public required string OriginalFileName { get; init; }
    public required string Extension { get; init; }
}
