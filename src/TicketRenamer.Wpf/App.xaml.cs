using System.IO;
using System.Net.Http.Headers;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Parsers;
using TicketRenamer.Core.Services;
using TicketRenamer.Wpf.Services;
using TicketRenamer.Wpf.ViewModels;
using TicketRenamer.Wpf.Views;

namespace TicketRenamer.Wpf;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var section = context.Configuration.GetSection("TicketRenamer");

                // Core services
                services.AddHttpClient<IOcrService, GroqVisionService>((sp, client) =>
                {
                    var settingsService = sp.GetRequiredService<ISettingsService>();
                    var apiKey = settingsService.GroqApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey))
                        apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                });

                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<ILogService>(sp =>
                {
                    var settings = sp.GetRequiredService<ISettingsService>();
                    return new LogService(settings.LogFilePath);
                });
                services.AddSingleton<ProviderMatcher>(sp =>
                {
                    var settings = sp.GetRequiredService<ISettingsService>();
                    var dictionary = LoadProviderDictionary(settings.ProviderDictionaryPath);
                    return new ProviderMatcher(dictionary);
                });
                services.AddSingleton<IProcessingPipeline>(sp =>
                    new ProcessingPipeline(
                        sp.GetRequiredService<IOcrService>(),
                        sp.GetRequiredService<IBackupService>(),
                        sp.GetRequiredService<ILogService>(),
                        sp.GetRequiredService<ProviderMatcher>()));

                // WPF services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IDialogService, DialogService>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Windows
                services.AddTransient<MainWindow>();
            })
            .Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    private static ProviderDictionary LoadProviderDictionary(string path)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(path));
            if (File.Exists(altPath))
                path = altPath;
            else
                return new ProviderDictionary();
        }

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<ProviderDictionary>(json) ?? new ProviderDictionary();
    }
}
