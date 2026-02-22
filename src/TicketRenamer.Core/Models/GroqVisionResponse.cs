namespace TicketRenamer.Core.Models;

public sealed record GroqVisionResponse
{
    public string? Provider { get; init; }
    public string? Date { get; init; }
}
