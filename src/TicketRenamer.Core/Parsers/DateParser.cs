using System.Globalization;
using System.Text.RegularExpressions;

namespace TicketRenamer.Core.Parsers;

public static partial class DateParser
{
    private static readonly Dictionary<string, int> SpanishMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1, ["ene"] = 1,
        ["febrero"] = 2, ["feb"] = 2,
        ["marzo"] = 3, ["mar"] = 3,
        ["abril"] = 4, ["abr"] = 4,
        ["mayo"] = 5, ["may"] = 5,
        ["junio"] = 6, ["jun"] = 6,
        ["julio"] = 7, ["jul"] = 7,
        ["agosto"] = 8, ["ago"] = 8,
        ["septiembre"] = 9, ["sep"] = 9, ["sept"] = 9,
        ["octubre"] = 10, ["oct"] = 10,
        ["noviembre"] = 11, ["nov"] = 11,
        ["diciembre"] = 12, ["dic"] = 12,
    };

    public static DateOnly? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Try ISO format first: YYYY-MM-DD or YYYY/MM/DD
        var isoMatch = IsoDateRegex().Match(text);
        if (isoMatch.Success)
        {
            if (TryBuildDate(
                int.Parse(isoMatch.Groups[1].Value),
                int.Parse(isoMatch.Groups[2].Value),
                int.Parse(isoMatch.Groups[3].Value),
                out var isoDate))
                return isoDate;
        }

        // Try DD/MM/YYYY or DD-MM-YYYY
        var dmy = DmyDateRegex().Match(text);
        if (dmy.Success)
        {
            if (TryBuildDate(
                int.Parse(dmy.Groups[3].Value),
                int.Parse(dmy.Groups[2].Value),
                int.Parse(dmy.Groups[1].Value),
                out var dmyDate))
                return dmyDate;
        }

        // Try "DD mes YYYY" (Spanish month names)
        var spanishMatch = SpanishDateRegex().Match(text);
        if (spanishMatch.Success)
        {
            var monthName = spanishMatch.Groups[2].Value;
            if (SpanishMonths.TryGetValue(monthName, out var month))
            {
                if (TryBuildDate(
                    int.Parse(spanishMatch.Groups[3].Value),
                    month,
                    int.Parse(spanishMatch.Groups[1].Value),
                    out var spanishDate))
                    return spanishDate;
            }
        }

        return null;
    }

    private static bool TryBuildDate(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (year < 2000 || year > 2099 || month < 1 || month > 12 || day < 1 || day > 31)
            return false;

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"\b(\d{4})[/-](\d{2})[/-](\d{2})\b", RegexOptions.Compiled)]
    private static partial Regex IsoDateRegex();

    [GeneratedRegex(@"\b(\d{2})[/-](\d{2})[/-](\d{4})\b", RegexOptions.Compiled)]
    private static partial Regex DmyDateRegex();

    [GeneratedRegex(@"\b(\d{1,2})\s+(?:de\s+)?(\w+)\s+(?:de\s+)?(\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SpanishDateRegex();
}
