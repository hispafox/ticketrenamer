using FluentAssertions;
using TicketRenamer.Core.Parsers;

namespace TicketRenamer.Core.Tests.Parsers;

public class DateParserTests
{
    [Theory]
    [InlineData("2026-02-15", 2026, 2, 15)]
    [InlineData("2024-12-31", 2024, 12, 31)]
    [InlineData("2026/01/05", 2026, 1, 5)]
    public void Parse_IsoFormat_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        var result = DateParser.Parse(input);
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("15/02/2026", 2026, 2, 15)]
    [InlineData("31/12/2024", 2024, 12, 31)]
    [InlineData("01-01-2025", 2025, 1, 1)]
    [InlineData("05/03/2026", 2026, 3, 5)]
    public void Parse_DmyFormat_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        var result = DateParser.Parse(input);
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("15 febrero 2026", 2026, 2, 15)]
    [InlineData("1 ene 2025", 2025, 1, 1)]
    [InlineData("5 de marzo de 2026", 2026, 3, 5)]
    [InlineData("31 dic 2024", 2024, 12, 31)]
    [InlineData("10 de septiembre de 2025", 2025, 9, 10)]
    public void Parse_SpanishFormat_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        var result = DateParser.Parse(input);
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("Fecha: 15/02/2026 Total: 45.30", 2026, 2, 15)]
    [InlineData("MERCADONA S.A.\n15/02/2026\nTotal: 23.50", 2026, 2, 15)]
    public void Parse_DateInLargerText_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        var result = DateParser.Parse(input);
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no date here")]
    [InlineData("32/13/2026")]
    [InlineData("MERCADONA")]
    public void Parse_InvalidInput_ReturnsNull(string? input)
    {
        var result = DateParser.Parse(input);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidDate_February30_ReturnsNull()
    {
        var result = DateParser.Parse("30/02/2026");
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_YearOutOfRange_ReturnsNull()
    {
        var result = DateParser.Parse("15/02/1999");
        result.Should().BeNull();
    }
}
