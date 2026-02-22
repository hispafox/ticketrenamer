using FluentAssertions;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;

namespace TicketRenamer.Core.Tests.Parsers;

public class FileNameBuilderTests
{
    [Fact]
    public void BuildPreview_ReturnsCorrectFormat()
    {
        var data = new ReceiptData
        {
            Provider = "Mercadona",
            Date = new DateOnly(2026, 2, 15),
            OriginalFileName = "IMG_001.jpg",
            Extension = ".jpg"
        };

        var result = FileNameBuilder.BuildPreview(data);
        result.Should().Be("Mercadona-26-02-15.jpg");
    }

    [Fact]
    public void BuildPreview_ExtensionWithoutDot_StillWorks()
    {
        var data = new ReceiptData
        {
            Provider = "Lidl",
            Date = new DateOnly(2025, 12, 31),
            OriginalFileName = "photo.png",
            Extension = "png"
        };

        var result = FileNameBuilder.BuildPreview(data);
        result.Should().Be("Lidl-25-12-31.png");
    }

    [Fact]
    public void Build_NoCollision_ReturnsBaseName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var data = new ReceiptData
            {
                Provider = "Carrefour",
                Date = new DateOnly(2026, 3, 10),
                OriginalFileName = "IMG_002.jpg",
                Extension = ".jpg"
            };

            var result = FileNameBuilder.Build(data, tempDir);
            result.Should().Be("Carrefour-26-03-10.jpg");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Build_WithCollision_AddsSuffix()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create existing file to cause collision
            File.WriteAllText(Path.Combine(tempDir, "Mercadona-26-02-15.jpg"), "dummy");

            var data = new ReceiptData
            {
                Provider = "Mercadona",
                Date = new DateOnly(2026, 2, 15),
                OriginalFileName = "IMG_003.jpg",
                Extension = ".jpg"
            };

            var result = FileNameBuilder.Build(data, tempDir);
            result.Should().Be("Mercadona-26-02-15-1.jpg");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Build_MultipleCollisions_IncrementsCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "Dia-26-01-01.png"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "Dia-26-01-01-1.png"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "Dia-26-01-01-2.png"), "dummy");

            var data = new ReceiptData
            {
                Provider = "Dia",
                Date = new DateOnly(2026, 1, 1),
                OriginalFileName = "IMG_004.png",
                Extension = ".png"
            };

            var result = FileNameBuilder.Build(data, tempDir);
            result.Should().Be("Dia-26-01-01-3.png");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
