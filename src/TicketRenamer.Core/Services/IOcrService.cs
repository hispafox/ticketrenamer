using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Services;

public interface IOcrService
{
    Task<GroqVisionResponse> ExtractReceiptDataAsync(string imagePath, CancellationToken ct = default);
}
