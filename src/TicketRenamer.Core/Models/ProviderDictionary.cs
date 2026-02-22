namespace TicketRenamer.Core.Models;

public sealed record ProviderDictionary
{
    public List<ProviderMapping> Providers { get; init; } = [];
}

public sealed record ProviderMapping
{
    public List<string> Names { get; init; } = [];
    public required string NormalizedName { get; init; }
}
