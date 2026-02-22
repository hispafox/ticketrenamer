using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketRenamer.Wpf.Services;

namespace TicketRenamer.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private string _inputFolder = "";
    [ObservableProperty] private string _outputFolder = "";
    [ObservableProperty] private string _backupFolder = "";
    [ObservableProperty] private string _logFilePath = "";
    [ObservableProperty] private string _providerDictionaryPath = "";
    [ObservableProperty] private string _groqApiKey = "";
    [ObservableProperty] private bool _verbose = true;
    [ObservableProperty] private bool _saved;

    public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
    }

    public void LoadFromSettings()
    {
        InputFolder = _settingsService.InputFolder;
        OutputFolder = _settingsService.OutputFolder;
        BackupFolder = _settingsService.BackupFolder;
        LogFilePath = _settingsService.LogFilePath;
        ProviderDictionaryPath = _settingsService.ProviderDictionaryPath;
        GroqApiKey = _settingsService.GroqApiKey;
        Verbose = _settingsService.Verbose;
        Saved = false;
    }

    [RelayCommand]
    private void Save()
    {
        _settingsService.UpdateFrom(
            InputFolder, OutputFolder, BackupFolder,
            LogFilePath, ProviderDictionaryPath, GroqApiKey, Verbose);
        Saved = true;
    }

    [RelayCommand]
    private void BrowseInputFolder()
    {
        var folder = _dialogService.BrowseFolder("Seleccionar carpeta de entrada");
        if (folder is not null) InputFolder = folder;
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var folder = _dialogService.BrowseFolder("Seleccionar carpeta de procesados");
        if (folder is not null) OutputFolder = folder;
    }

    [RelayCommand]
    private void BrowseBackupFolder()
    {
        var folder = _dialogService.BrowseFolder("Seleccionar carpeta de backup");
        if (folder is not null) BackupFolder = folder;
    }
}
