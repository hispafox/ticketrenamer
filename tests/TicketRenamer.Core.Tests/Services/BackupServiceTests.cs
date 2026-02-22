using FluentAssertions;
using TicketRenamer.Core.Services;

namespace TicketRenamer.Core.Tests.Services;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _inputDir;
    private readonly string _backupDir;

    public BackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _inputDir = Path.Combine(_tempDir, "entrada");
        _backupDir = Path.Combine(_tempDir, "backup");
        Directory.CreateDirectory(_inputDir);
    }

    [Fact]
    public async Task BackupFileAsync_CopiesFileToBackup()
    {
        var service = new BackupService();
        var sourceFile = Path.Combine(_inputDir, "test.jpg");
        await File.WriteAllBytesAsync(sourceFile, new byte[] { 1, 2, 3, 4, 5 });

        await service.BackupFileAsync(sourceFile, _backupDir);

        var backupFile = Path.Combine(_backupDir, "test.jpg");
        File.Exists(backupFile).Should().BeTrue();
    }

    [Fact]
    public async Task BackupFileAsync_PreservesFileSize()
    {
        var service = new BackupService();
        var content = new byte[1024];
        Random.Shared.NextBytes(content);

        var sourceFile = Path.Combine(_inputDir, "photo.png");
        await File.WriteAllBytesAsync(sourceFile, content);

        await service.BackupFileAsync(sourceFile, _backupDir);

        var backupFile = Path.Combine(_backupDir, "photo.png");
        new FileInfo(backupFile).Length.Should().Be(new FileInfo(sourceFile).Length);
    }

    [Fact]
    public void VerifyBackup_MatchingSizes_ReturnsTrue()
    {
        var service = new BackupService();
        Directory.CreateDirectory(_backupDir);

        var sourceFile = Path.Combine(_inputDir, "test.jpg");
        var backupFile = Path.Combine(_backupDir, "test.jpg");
        File.WriteAllBytes(sourceFile, [1, 2, 3]);
        File.WriteAllBytes(backupFile, [1, 2, 3]);

        service.VerifyBackup(sourceFile, backupFile).Should().BeTrue();
    }

    [Fact]
    public void VerifyBackup_DifferentSizes_ReturnsFalse()
    {
        var service = new BackupService();
        Directory.CreateDirectory(_backupDir);

        var sourceFile = Path.Combine(_inputDir, "test.jpg");
        var backupFile = Path.Combine(_backupDir, "test.jpg");
        File.WriteAllBytes(sourceFile, [1, 2, 3]);
        File.WriteAllBytes(backupFile, [1, 2]);

        service.VerifyBackup(sourceFile, backupFile).Should().BeFalse();
    }

    [Fact]
    public void VerifyBackup_MissingBackup_ReturnsFalse()
    {
        var service = new BackupService();
        var sourceFile = Path.Combine(_inputDir, "test.jpg");
        File.WriteAllBytes(sourceFile, [1, 2, 3]);

        service.VerifyBackup(sourceFile, Path.Combine(_backupDir, "nonexistent.jpg")).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
