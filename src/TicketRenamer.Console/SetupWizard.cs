using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TicketRenamer.Console;

public static class SetupWizard
{
    public static void Run(string configPath)
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════╗");
        System.Console.WriteLine("║   TicketRenamer - Configuracion          ║");
        System.Console.WriteLine("╚══════════════════════════════════════════╝");
        System.Console.WriteLine();

        // Load existing config or create defaults
        var config = LoadOrCreateConfig(configPath);

        var running = true;
        while (running)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("  Configuracion actual:");
            System.Console.WriteLine($"  1. Carpeta entrada:     {config["InputFolder"]}");
            System.Console.WriteLine($"  2. Carpeta procesados:  {config["OutputFolder"]}");
            System.Console.WriteLine($"  3. Carpeta backup:      {config["BackupFolder"]}");
            System.Console.WriteLine($"  4. Archivo de registro: {config["LogFilePath"]}");
            System.Console.WriteLine($"  5. Diccionario:         {config["ProviderDictionaryPath"]}");
            System.Console.WriteLine($"  6. Groq API Key:        {MaskApiKey(config["GroqApiKey"]?.ToString())}");
            System.Console.WriteLine($"  7. Modo verbose:        {config["Verbose"]}");
            System.Console.WriteLine($"  8. Modo watch:          {config["Watch"]}");
            System.Console.WriteLine($"  9. Modo dry-run:        {config["DryRun"]}");
            System.Console.WriteLine();
            System.Console.WriteLine("  0. Guardar y salir");
            System.Console.WriteLine("  Q. Salir sin guardar");
            System.Console.WriteLine();
            System.Console.Write("  Selecciona opcion: ");

            var key = System.Console.ReadLine()?.Trim();

            switch (key)
            {
                case "1":
                    config["InputFolder"] = AskPath("Carpeta de entrada", config["InputFolder"]?.ToString());
                    break;
                case "2":
                    config["OutputFolder"] = AskPath("Carpeta de procesados", config["OutputFolder"]?.ToString());
                    break;
                case "3":
                    config["BackupFolder"] = AskPath("Carpeta de backup", config["BackupFolder"]?.ToString());
                    break;
                case "4":
                    config["LogFilePath"] = AskPath("Archivo de registro", config["LogFilePath"]?.ToString());
                    break;
                case "5":
                    config["ProviderDictionaryPath"] = AskPath("Diccionario de proveedores", config["ProviderDictionaryPath"]?.ToString());
                    break;
                case "6":
                    config["GroqApiKey"] = AskString("Groq API Key", config["GroqApiKey"]?.ToString());
                    break;
                case "7":
                    config["Verbose"] = ToggleBool(config["Verbose"]);
                    System.Console.WriteLine($"  Verbose: {config["Verbose"]}");
                    break;
                case "8":
                    config["Watch"] = ToggleBool(config["Watch"]);
                    System.Console.WriteLine($"  Watch: {config["Watch"]}");
                    break;
                case "9":
                    config["DryRun"] = ToggleBool(config["DryRun"]);
                    System.Console.WriteLine($"  Dry-run: {config["DryRun"]}");
                    break;
                case "0":
                    SaveConfig(configPath, config);
                    System.Console.WriteLine("  Configuracion guardada.");
                    running = false;
                    break;
                case "Q":
                case "q":
                    running = false;
                    break;
            }
        }
    }

    private static JObject LoadOrCreateConfig(string configPath)
    {
        JObject root;
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            root = JObject.Parse(json);
        }
        else
        {
            root = new JObject();
        }

        var section = root["TicketRenamer"] as JObject ?? new JObject();

        // Ensure all keys exist with defaults
        section["InputFolder"] ??= @"C:\Tickets\entrada";
        section["OutputFolder"] ??= @"C:\Tickets\procesados";
        section["BackupFolder"] ??= @"C:\Tickets\backup";
        section["LogFilePath"] ??= @"C:\Tickets\registro.txt";
        section["ProviderDictionaryPath"] ??= "proveedores.json";
        section["GroqApiKey"] ??= Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        section["DryRun"] ??= false;
        section["Verbose"] ??= true;
        section["Watch"] ??= false;

        return section;
    }

    private static void SaveConfig(string configPath, JObject section)
    {
        var root = new JObject { ["TicketRenamer"] = section };
        var json = root.ToString(Formatting.Indented);
        File.WriteAllText(configPath, json);
    }

    private static string AskPath(string label, string? current)
    {
        System.Console.Write($"  {label} [{current}]: ");
        var input = System.Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? current ?? "" : input;
    }

    private static string AskString(string label, string? current)
    {
        var masked = MaskApiKey(current);
        System.Console.Write($"  {label} [{masked}]: ");
        var input = System.Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? current ?? "" : input;
    }

    private static bool ToggleBool(JToken? token)
    {
        var current = token?.Value<bool>() ?? false;
        return !current;
    }

    private static string MaskApiKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return "(no configurada)";
        if (key.Length <= 8)
            return "****";
        return key[..4] + "..." + key[^4..];
    }
}
