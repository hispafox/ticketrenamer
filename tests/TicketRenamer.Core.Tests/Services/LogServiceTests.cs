using FluentAssertions;
using TicketRenamer.Core.Services;

namespace TicketRenamer.Core.Tests.Services;

public class LogServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _logPath;

    public LogServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _logPath = Path.Combine(_tempDir, "registro.txt");
    }

    [Fact]
    public async Task LogOperationAsync_Success_WritesCorrectFormat()
    {
        var service = new LogService(_logPath);
        await service.LogOperationAsync("IMG_001.jpg", "Mercadona-26-02-15.jpg", true);

        var content = await File.ReadAllTextAsync(_logPath);
        content.Should().Contain("IMG_001.jpg");
        content.Should().Contain("Mercadona-26-02-15.jpg");
        content.Should().Contain("OK");
        content.Should().Contain("|");
    }

    [Fact]
    public async Task LogOperationAsync_Error_WritesErrorMessage()
    {
        var service = new LogService(_logPath);
        await service.LogOperationAsync("IMG_002.jpg", null, false, "OCR failed");

        var content = await File.ReadAllTextAsync(_logPath);
        content.Should().Contain("IMG_002.jpg");
        content.Should().Contain("ERROR: OCR failed");
    }

    [Fact]
    public async Task LoadProcessedFilesAsync_ReturnsOnlySuccessful()
    {
        var service = new LogService(_logPath);
        await service.LogOperationAsync("IMG_001.jpg", "Mercadona-26-02-15.jpg", true);
        await service.LogOperationAsync("IMG_002.jpg", null, false, "OCR failed");
        await service.LogOperationAsync("IMG_003.jpg", "Lidl-26-03-10.jpg", true);

        var processed = await service.LoadProcessedFilesAsync();
        processed.Should().Contain("IMG_001.jpg");
        processed.Should().Contain("IMG_003.jpg");
        processed.Should().NotContain("IMG_002.jpg");
    }

    [Fact]
    public async Task LoadProcessedFilesAsync_NoFile_ReturnsEmpty()
    {
        var service = new LogService(Path.Combine(_tempDir, "nonexistent.txt"));
        var processed = await service.LoadProcessedFilesAsync();
        processed.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
