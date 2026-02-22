namespace TicketRenamer.Core.Services;

public interface IBackupService
{
    Task BackupFileAsync(string sourceFile, string backupFolder, CancellationToken ct = default);
    bool VerifyBackup(string originalFile, string backupFile);
}
