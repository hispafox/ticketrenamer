namespace TicketRenamer.Core.Models;

public enum ProcessingStatus
{
    Success,
    OcrFailed,
    DateNotFound,
    ProviderNotFound,
    BackupFailed,
    AlreadyProcessed,
    InvalidImage
}
