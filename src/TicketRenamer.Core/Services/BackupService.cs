namespace TicketRenamer.Core.Services;

public sealed class BackupService : IBackupService
{
    public Task BackupFileAsync(string sourceFile, string backupFolder, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(backupFolder);

        var fileName = Path.GetFileName(sourceFile);
        var destPath = Path.Combine(backupFolder, fileName);

        File.Copy(sourceFile, destPath, overwrite: true);

        if (!VerifyBackup(sourceFile, destPath))
            throw new IOException($"Backup verification failed for '{fileName}': file sizes do not match.");

        return Task.CompletedTask;
    }

    public bool VerifyBackup(string originalFile, string backupFile)
    {
        if (!File.Exists(backupFile))
            return false;

        var originalSize = new FileInfo(originalFile).Length;
        var backupSize = new FileInfo(backupFile).Length;
        return originalSize == backupSize;
    }
}
