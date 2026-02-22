using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Services;

public interface IProcessingPipeline
{
    Task<List<ProcessingResult>> ProcessAllAsync(ProcessingOptions options, CancellationToken ct = default);
}
