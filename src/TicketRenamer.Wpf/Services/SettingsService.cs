using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TicketRenamer.Core.Models;

namespace TicketRenamer.Wpf.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _configPath;
    private JObject _section;

    public string InputFolder => _section["InputFolder"]?.ToString() ?? @"C:\Tickets\entrada";
    public string OutputFolder => _section["OutputFolder"]?.ToString() ?? @"C:\Tickets\procesados";
    public string BackupFolder => _section["BackupFolder"]?.ToString() ?? @"C:\Tickets\backup";
    public string LogFilePath => _section["LogFilePath"]?.ToString() ?? @"C:\Tickets\registro.txt";
    public string ProviderDictionaryPath => _section["ProviderDictionaryPath"]?.ToString() ?? "proveedores.json";
    public string GroqApiKey => _section["GroqApiKey"]?.ToString() ?? "";
    public bool Verbose => _section["Verbose"]?.Value<bool>() ?? true;
    public bool DryRun => _section["DryRun"]?.Value<bool>() ?? false;
    public bool Watch => _section["Watch"]?.Value<bool>() ?? false;

    public SettingsService(IConfiguration configuration)
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _section = new JObject();
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_configPath))
        {
            _section = CreateDefaults();
            return;
        }

        var json = File.ReadAllText(_configPath);
        var root = JObject.Parse(json);
        _section = root["TicketRenamer"] as JObject ?? CreateDefaults();
    }

    public void Save()
    {
        var root = new JObject { ["TicketRenamer"] = _section };
        var json = root.ToString(Formatting.Indented);
        File.WriteAllText(_configPath, json);
    }

    public void UpdateFrom(string inputFolder, string outputFolder, string backupFolder,
        string logFilePath, string providerDictionaryPath, string groqApiKey, bool verbose)
    {
        _section["InputFolder"] = inputFolder;
        _section["OutputFolder"] = outputFolder;
        _section["BackupFolder"] = backupFolder;
        _section["LogFilePath"] = logFilePath;
        _section["ProviderDictionaryPath"] = providerDictionaryPath;
        _section["GroqApiKey"] = groqApiKey;
        _section["Verbose"] = verbose;
        Save();
    }

    public ProcessingOptions BuildProcessingOptions() => new()
    {
        InputFolder = InputFolder,
        OutputFolder = OutputFolder,
        BackupFolder = BackupFolder,
        LogFilePath = LogFilePath,
        ProviderDictionaryPath = ProviderDictionaryPath,
        DryRun = DryRun,
        Verbose = Verbose
    };

    private static JObject CreateDefaults() => new()
    {
        ["InputFolder"] = @"C:\Tickets\entrada",
        ["OutputFolder"] = @"C:\Tickets\procesados",
        ["BackupFolder"] = @"C:\Tickets\backup",
        ["LogFilePath"] = @"C:\Tickets\registro.txt",
        ["ProviderDictionaryPath"] = "proveedores.json",
        ["GroqApiKey"] = "",
        ["DryRun"] = false,
        ["Verbose"] = true,
        ["Watch"] = false
    };
}
