using FluentAssertions;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;

namespace TicketRenamer.Core.Tests.Parsers;

public class ProviderMatcherTests
{
    private static ProviderDictionary CreateTestDictionary() => new()
    {
        Providers =
        [
            new ProviderMapping { Names = ["MERCADONA", "MERCADONA S.A."], NormalizedName = "Mercadona" },
            new ProviderMapping { Names = ["CARREFOUR", "CARREFOUR EXPRESS"], NormalizedName = "Carrefour" },
            new ProviderMapping { Names = ["DIA", "DIA %"], NormalizedName = "Dia" },
            new ProviderMapping { Names = ["LIDL"], NormalizedName = "Lidl" },
            new ProviderMapping { Names = ["AHORRAMAS", "AHORRA MAS", "AHORAMAS"], NormalizedName = "Ahorramas" },
        ]
    };

    [Theory]
    [InlineData("MERCADONA S.A.", "Mercadona")]
    [InlineData("mercadona", "Mercadona")]
    [InlineData("CARREFOUR EXPRESS", "Carrefour")]
    [InlineData("DIA %", "Dia")]
    [InlineData("Factura de LIDL supermercados", "Lidl")]
    [InlineData("AHORRA MAS tienda", "Ahorramas")]
    public void Match_KnownProvider_ReturnsNormalized(string text, string expected)
    {
        var matcher = new ProviderMatcher(CreateTestDictionary());
        matcher.Match(text).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown Store")]
    [InlineData("Random text without provider")]
    public void Match_UnknownProvider_ReturnsNull(string? text)
    {
        var matcher = new ProviderMatcher(CreateTestDictionary());
        matcher.Match(text).Should().BeNull();
    }

    [Theory]
    [InlineData("MERCADONA", "Mercadona")]
    [InlineData("Carrefour Express", "Carrefour")]
    [InlineData("Some Random Store S.A.", "Some Random Store")]
    [InlineData("Tienda Local S.L.", "Tienda Local")]
    public void Normalize_ReturnsCleanName(string raw, string expected)
    {
        var matcher = new ProviderMatcher(CreateTestDictionary());
        matcher.Normalize(raw).Should().Be(expected);
    }

    [Fact]
    public void Normalize_EmptyString_ReturnsDesconocido()
    {
        var matcher = new ProviderMatcher(CreateTestDictionary());
        matcher.Normalize("").Should().Be("Desconocido");
    }
}
