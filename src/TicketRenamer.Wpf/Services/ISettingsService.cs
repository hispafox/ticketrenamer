using TicketRenamer.Core.Models;

namespace TicketRenamer.Wpf.Services;

public interface ISettingsService
{
    string InputFolder { get; }
    string OutputFolder { get; }
    string BackupFolder { get; }
    string LogFilePath { get; }
    string ProviderDictionaryPath { get; }
    string GroqApiKey { get; }
    bool Verbose { get; }
    bool DryRun { get; }
    bool Watch { get; }

    ProcessingOptions BuildProcessingOptions();
    void Load();
    void Save();
    void UpdateFrom(string inputFolder, string outputFolder, string backupFolder,
        string logFilePath, string providerDictionaryPath, string groqApiKey, bool verbose);
}
