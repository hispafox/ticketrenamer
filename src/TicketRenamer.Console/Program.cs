using System.CommandLine;
using Newtonsoft.Json;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;
using TicketRenamer.Core.Services;

var inputOption = new Option<DirectoryInfo>("--input", "Input folder with ticket images") { IsRequired = true };
var outputOption = new Option<DirectoryInfo>("--output", "Output folder for renamed files") { IsRequired = true };
var backupOption = new Option<DirectoryInfo>("--backup", "Backup folder for originals") { IsRequired = true };
var logOption = new Option<FileInfo?>("--log", "Path to log file (default: registro.txt in parent folder)");
var dryRunOption = new Option<bool>("--dry-run", "Simulate without moving files");
var verboseOption = new Option<bool>("--verbose", "Show detailed processing info");

var rootCommand = new RootCommand("TicketRenamer - Intelligent ticket renaming system using OCR")
{
    inputOption, outputOption, backupOption, logOption, dryRunOption, verboseOption
};

rootCommand.SetHandler(async (input, output, backup, log, dryRun, verbose) =>
{
    var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.Error.WriteLine("ERROR: GROQ_API_KEY environment variable is not set.");
        Console.Error.WriteLine("Set it with: set GROQ_API_KEY=your_api_key_here");
        Environment.ExitCode = 2;
        return;
    }

    var logPath = log?.FullName ?? Path.Combine(input.Parent?.FullName ?? input.FullName, "registro.txt");

    var options = new ProcessingOptions
    {
        InputFolder = input.FullName,
        OutputFolder = output.FullName,
        BackupFolder = backup.FullName,
        LogFilePath = logPath,
        DryRun = dryRun,
        Verbose = verbose
    };

    // Load provider dictionary
    var dictionary = LoadProviderDictionary(options.ProviderDictionaryPath);

    // Setup services
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

    var ocrService = new GroqVisionService(httpClient);
    var backupService = new BackupService();
    var logService = new LogService(logPath);
    var providerMatcher = new ProviderMatcher(dictionary);

    Action<string>? logger = verbose ? Console.WriteLine : null;

    var pipeline = new ProcessingPipeline(ocrService, backupService, logService, providerMatcher, logger);

    if (dryRun)
        Console.WriteLine("[DRY-RUN MODE] No files will be moved.");

    Console.WriteLine($"Input:   {options.InputFolder}");
    Console.WriteLine($"Output:  {options.OutputFolder}");
    Console.WriteLine($"Backup:  {options.BackupFolder}");
    Console.WriteLine($"Log:     {logPath}");
    Console.WriteLine();

    try
    {
        var results = await pipeline.ProcessAllAsync(options);

        var successCount = results.Count(r => r.Status == ProcessingStatus.Success);
        var failCount = results.Count(r => r.Status != ProcessingStatus.Success);

        Console.WriteLine();
        Console.WriteLine($"Results: {successCount} succeeded, {failCount} failed out of {results.Count} total.");

        foreach (var result in results.Where(r => r.Status != ProcessingStatus.Success))
        {
            Console.WriteLine($"  FAILED: {result.OriginalFileName} - {result.Status}: {result.ErrorMessage}");
        }

        if (results.Any(r => r.Status == ProcessingStatus.BackupFailed))
            Environment.ExitCode = 2;
        else if (failCount > 0)
            Environment.ExitCode = 1;
        else
            Environment.ExitCode = 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"CRITICAL ERROR: {ex.Message}");
        Environment.ExitCode = 2;
    }

}, inputOption, outputOption, backupOption, logOption, dryRunOption, verboseOption);

return await rootCommand.InvokeAsync(args);

static ProviderDictionary LoadProviderDictionary(string path)
{
    if (!File.Exists(path))
    {
        // Try next to the executable
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
