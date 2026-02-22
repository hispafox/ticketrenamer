using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Services;

public interface IProcessingPipeline
{
    Task<List<ProcessingResult>> ProcessAllAsync(ProcessingOptions options, CancellationToken ct = default);
    Task<List<ProcessingResult>> ProcessAllAsync(ProcessingOptions options, IProgress<ProcessingResult>? progress, CancellationToken ct = default);
}
