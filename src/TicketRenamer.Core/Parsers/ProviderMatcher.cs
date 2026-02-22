using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Parsers;

public sealed class ProviderMatcher
{
    private readonly ProviderDictionary _dictionary;

    public ProviderMatcher(ProviderDictionary dictionary)
    {
        _dictionary = dictionary;
    }

    public string? Match(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return null;

        // Try exact match (case-insensitive) against known provider names
        foreach (var mapping in _dictionary.Providers)
        {
            foreach (var name in mapping.Names)
            {
                if (ocrText.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return mapping.NormalizedName;
            }
        }

        return null;
    }

    public string Normalize(string rawProvider)
    {
        if (string.IsNullOrWhiteSpace(rawProvider))
            return CleanProviderName(rawProvider);

        // Try to find in dictionary first
        foreach (var mapping in _dictionary.Providers)
        {
            foreach (var name in mapping.Names)
            {
                if (rawProvider.Contains(name, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(rawProvider, StringComparison.OrdinalIgnoreCase))
                    return mapping.NormalizedName;
            }
        }

        // If not found, clean up the raw name: capitalize, remove special chars
        return CleanProviderName(rawProvider);
    }

    private static string CleanProviderName(string raw)
    {
        var cleaned = raw.Trim();
        if (cleaned.Length == 0)
            return "Desconocido";

        // Remove S.A., S.L., trailing commas and dots, etc.
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @",?\s*\bS\.?\s*A\.?\b\.?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @",?\s*\bS\.?\s*L\.?\b\.?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        cleaned = cleaned.TrimEnd('.', ',', ' ');

        // Title case
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
    }
}
