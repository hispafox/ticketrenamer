namespace TicketRenamer.Core.Models;

public sealed record ProcessingOptions
{
    public required string InputFolder { get; init; }
    public required string OutputFolder { get; init; }
    public required string BackupFolder { get; init; }
    public string LogFilePath { get; init; } = "registro.txt";
    public string ProviderDictionaryPath { get; init; } = "proveedores.json";
    public bool DryRun { get; init; }
    public bool Verbose { get; init; }

    public static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png"];
}
