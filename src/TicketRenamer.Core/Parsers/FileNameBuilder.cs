using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Parsers;

public static class FileNameBuilder
{
    /// <summary>
    /// Builds the target file name in format: Proveedor-AA-MM-DD[-N].ext
    /// Adds numeric suffix (-1, -2, etc.) if collision exists in outputFolder.
    /// </summary>
    public static string Build(ReceiptData data, string outputFolder)
    {
        var dateStr = data.Date.ToString("yy-MM-dd");
        var baseName = $"{data.Provider}-{dateStr}";
        var extension = data.Extension.StartsWith('.') ? data.Extension : $".{data.Extension}";

        var candidate = $"{baseName}{extension}";
        var fullPath = Path.Combine(outputFolder, candidate);

        if (!File.Exists(fullPath))
            return candidate;

        // Collision: add numeric suffix
        for (var i = 1; i < 10000; i++)
        {
            candidate = $"{baseName}-{i}{extension}";
            fullPath = Path.Combine(outputFolder, candidate);
            if (!File.Exists(fullPath))
                return candidate;
        }

        throw new InvalidOperationException($"Too many collisions for base name '{baseName}'");
    }

    /// <summary>
    /// Builds the file name without checking for collisions (for dry-run/preview).
    /// </summary>
    public static string BuildPreview(ReceiptData data)
    {
        var dateStr = data.Date.ToString("yy-MM-dd");
        var extension = data.Extension.StartsWith('.') ? data.Extension : $".{data.Extension}";
        return $"{data.Provider}-{dateStr}{extension}";
    }
}
