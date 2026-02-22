using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TicketRenamer.Console;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;
using TicketRenamer.Core.Services;

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

// --setup flag: open configuration menu
if (args.Contains("--setup"))
{
    SetupWizard.Run(configPath);
    return 0;
}

// Load configuration from appsettings.json + environment variables
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var section = configuration.GetSection("TicketRenamer");

var inputFolder = section["InputFolder"] ?? @"C:\Tickets\entrada";
var outputFolder = section["OutputFolder"] ?? @"C:\Tickets\procesados";
var backupFolder = section["BackupFolder"] ?? @"C:\Tickets\backup";
var logFilePath = section["LogFilePath"] ?? @"C:\Tickets\registro.txt";
var providerDictPath = section["ProviderDictionaryPath"] ?? "proveedores.json";
var dryRun = bool.TryParse(section["DryRun"], out var dr) && dr;
var verbose = !bool.TryParse(section["Verbose"], out var vb) || vb; // default true
var watch = bool.TryParse(section["Watch"], out var w) && w;

// API key: appsettings.json > environment variable
var apiKey = section["GroqApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
    apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

// CLI overrides (simple flags, not System.CommandLine - keep it lightweight)
if (args.Contains("--dry-run")) dryRun = true;
if (args.Contains("--verbose")) verbose = true;
if (args.Contains("--watch")) watch = true;
if (args.Contains("--quiet")) verbose = false;

// First-time setup: if no config exists and no API key, launch wizard
if (!File.Exists(configPath) || string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("No se encontro configuracion o falta la API key de Groq.");
    Console.Write("Deseas configurar ahora? (S/n): ");
    var answer = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(answer) || answer.StartsWith("s", StringComparison.OrdinalIgnoreCase))
    {
        SetupWizard.Run(configPath);

        // Reload config after setup
        configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        section = configuration.GetSection("TicketRenamer");
        inputFolder = section["InputFolder"] ?? inputFolder;
        outputFolder = section["OutputFolder"] ?? outputFolder;
        backupFolder = section["BackupFolder"] ?? backupFolder;
        logFilePath = section["LogFilePath"] ?? logFilePath;
        providerDictPath = section["ProviderDictionaryPath"] ?? providerDictPath;
        dryRun = bool.TryParse(section["DryRun"], out dr) && dr;
        verbose = !bool.TryParse(section["Verbose"], out vb) || vb;
        watch = bool.TryParse(section["Watch"], out w) && w;
        apiKey = section["GroqApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
    }
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("ERROR: No se ha configurado la API key de Groq.");
    Console.Error.WriteLine("Ejecuta con --setup para configurar, o establece GROQ_API_KEY como variable de entorno.");
    return 2;
}

var options = new ProcessingOptions
{
    InputFolder = inputFolder,
    OutputFolder = outputFolder,
    BackupFolder = backupFolder,
    LogFilePath = logFilePath,
    ProviderDictionaryPath = providerDictPath,
    DryRun = dryRun,
    Verbose = verbose
};

// Load provider dictionary
var dictionary = LoadProviderDictionary(providerDictPath);

// Setup services
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

var ocrService = new GroqVisionService(httpClient);
var backupService = new BackupService();
var logService = new LogService(logFilePath);
var providerMatcher = new ProviderMatcher(dictionary);
Action<string> logger = Console.WriteLine;

var pipeline = new ProcessingPipeline(ocrService, backupService, logService, providerMatcher, verbose ? logger : null);

// Display configuration
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║         TicketRenamer v1.0               ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.WriteLine($"  Entrada:    {options.InputFolder}");
Console.WriteLine($"  Procesados: {options.OutputFolder}");
Console.WriteLine($"  Backup:     {options.BackupFolder}");
Console.WriteLine($"  Registro:   {options.LogFilePath}");
if (dryRun) Console.WriteLine("  [MODO DRY-RUN] No se moveran archivos.");
if (watch) Console.WriteLine("  [MODO WATCH] Vigilando carpeta de entrada...");
Console.WriteLine();

if (watch)
{
    // Watch mode: monitor folder for new files
    using var watcher = new FolderWatcher(pipeline, options, logger);
    watcher.Start();

    // Process any existing files first
    await RunBatch(pipeline, options);

    // Wait until Ctrl+C
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("\nDeteniendo...");
    };

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }

    watcher.Stop();
    return 0;
}
else
{
    // Single batch mode
    return await RunBatch(pipeline, options);
}

static async Task<int> RunBatch(ProcessingPipeline pipeline, ProcessingOptions options)
{
    try
    {
        var results = await pipeline.ProcessAllAsync(options);

        var successCount = results.Count(r => r.Status == ProcessingStatus.Success);
        var failCount = results.Count(r => r.Status != ProcessingStatus.Success);

        Console.WriteLine();
        Console.WriteLine($"Resultado: {successCount} procesados, {failCount} fallidos de {results.Count} total.");

        foreach (var result in results.Where(r => r.Status != ProcessingStatus.Success))
        {
            Console.WriteLine($"  FALLO: {result.OriginalFileName} - {result.Status}: {result.ErrorMessage}");
        }

        if (results.Any(r => r.Status == ProcessingStatus.BackupFailed))
            return 2;
        return failCount > 0 ? 1 : 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"ERROR CRITICO: {ex.Message}");
        return 2;
    }
}

static ProviderDictionary LoadProviderDictionary(string path)
{
    if (!File.Exists(path))
    {
        var exeDir = AppContext.BaseDirectory;
        var altPath = Path.Combine(exeDir, Path.GetFileName(path));
        if (File.Exists(altPath))
            path = altPath;
        else
            return new ProviderDictionary();
    }

    var json = File.ReadAllText(path);
    return JsonConvert.DeserializeObject<ProviderDictionary>(json) ?? new ProviderDictionary();
}
