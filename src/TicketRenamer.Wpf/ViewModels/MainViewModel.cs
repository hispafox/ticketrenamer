using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketRenamer.Core.Models;
using TicketRenamer.Core.Services;
using TicketRenamer.Wpf.Services;

namespace TicketRenamer.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProcessingPipeline _pipeline;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cts;
    private FileSystemWatcher? _watcher;

    [ObservableProperty] private ObservableCollection<FileItemViewModel> _files = [];
    [ObservableProperty] private FileItemViewModel? _selectedFile;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isWatchMode;
    [ObservableProperty] private bool _isDryRun;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _processingCount;
    [ObservableProperty] private int _completedCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private string _statusMessage = "Listo";
    [ObservableProperty] private ObservableCollection<string> _logMessages = [];

    public string InputFolder => _settingsService.InputFolder;

    public MainViewModel(
        IProcessingPipeline pipeline,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _pipeline = pipeline;
        _settingsService = settingsService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private void RefreshFiles()
    {
        var inputFolder = _settingsService.InputFolder;
        if (!Directory.Exists(inputFolder))
        {
            Directory.CreateDirectory(inputFolder);
        }

        var existingNames = Files.ToDictionary(f => f.FileName, f => f);
        var currentFiles = Directory.GetFiles(inputFolder)
            .Where(f => ProcessingOptions.SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .ToHashSet();

        // Remove files no longer in folder
        var toRemove = Files.Where(f => !currentFiles.Contains(f.FileName)).ToList();
        foreach (var item in toRemove)
            Files.Remove(item);

        // Add new files
        foreach (var filePath in Directory.GetFiles(inputFolder)
            .Where(f => ProcessingOptions.SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant())))
        {
            var name = Path.GetFileName(filePath);
            if (!existingNames.ContainsKey(name))
            {
                Files.Add(FileItemViewModel.FromFile(filePath));
            }
        }

        UpdateCounts();
        StatusMessage = $"{Files.Count} archivo(s) en carpeta de entrada";
    }

    [RelayCommand(CanExecute = nameof(CanStartProcessing))]
    private async Task StartProcessingAsync()
    {
        IsProcessing = true;
        StatusMessage = "Procesando...";
        _cts = new CancellationTokenSource();

        foreach (var file in Files.Where(f => f.State == FileProcessingState.Pending))
            file.State = FileProcessingState.Processing;
        UpdateCounts();

        var options = _settingsService.BuildProcessingOptions();
        if (IsDryRun) options = options with { DryRun = true };

        var progress = new Progress<ProcessingResult>(result =>
        {
            var fileVm = Files.FirstOrDefault(f => f.FileName == result.OriginalFileName);
            fileVm?.UpdateFromResult(result);

            var logLine = result.Status == ProcessingStatus.Success
                ? $"OK: {result.OriginalFileName} -> {result.NewFileName}"
                : $"ERROR: {result.OriginalFileName} - {result.ErrorMessage}";
            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {logLine}");

            UpdateCounts();
        });

        try
        {
            var results = await _pipeline.ProcessAllAsync(options, progress, _cts.Token);

            var ok = results.Count(r => r.Status == ProcessingStatus.Success);
            var fail = results.Count(r => r.Status != ProcessingStatus.Success);
            StatusMessage = $"Completado: {ok} procesados, {fail} fallidos";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Procesamiento cancelado";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _dialogService.ShowError("Error de procesamiento", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            _cts.Dispose();
            _cts = null;
            UpdateCounts();
            StartProcessingCommand.NotifyCanExecuteChanged();
            StopProcessingCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStartProcessing() => !IsProcessing && Files.Any(f => f.State == FileProcessingState.Pending);

    [RelayCommand(CanExecute = nameof(CanStopProcessing))]
    private void StopProcessing()
    {
        _cts?.Cancel();
        StatusMessage = "Cancelando...";
    }

    private bool CanStopProcessing() => IsProcessing;

    [RelayCommand]
    private void ToggleWatchMode()
    {
        if (IsWatchMode)
        {
            StartWatching();
        }
        else
        {
            StopWatching();
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_dialogService.ShowSettingsWindow())
        {
            _settingsService.Load();
            RefreshFiles();
            OnPropertyChanged(nameof(InputFolder));
        }
    }

    public void HandleFileDrop(string[] files)
    {
        var inputFolder = _settingsService.InputFolder;
        Directory.CreateDirectory(inputFolder);

        var copied = 0;
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (!ProcessingOptions.SupportedExtensions.Contains(ext)) continue;

            var dest = Path.Combine(inputFolder, Path.GetFileName(file));
            if (!File.Exists(dest))
            {
                File.Copy(file, dest);
                copied++;
            }
        }

        if (copied > 0)
        {
            RefreshFiles();
            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {copied} archivo(s) copiados a carpeta de entrada");
        }
    }

    partial void OnSelectedFileChanged(FileItemViewModel? value)
    {
        if (value is not null && File.Exists(value.FilePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(value.FilePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 800;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage = bitmap;
            }
            catch
            {
                PreviewImage = null;
            }
        }
        else
        {
            PreviewImage = null;
        }

        StartProcessingCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        StartProcessingCommand.NotifyCanExecuteChanged();
        StopProcessingCommand.NotifyCanExecuteChanged();
    }

    private void UpdateCounts()
    {
        PendingCount = Files.Count(f => f.State == FileProcessingState.Pending);
        ProcessingCount = Files.Count(f => f.State == FileProcessingState.Processing);
        CompletedCount = Files.Count(f => f.State == FileProcessingState.Completed);
        FailedCount = Files.Count(f => f.State == FileProcessingState.Failed);
    }

    private void StartWatching()
    {
        var inputFolder = _settingsService.InputFolder;
        Directory.CreateDirectory(inputFolder);

        _watcher = new FileSystemWatcher(inputFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        foreach (var ext in ProcessingOptions.SupportedExtensions)
            _watcher.Filters.Add($"*{ext}");

        _watcher.Created += (_, _) =>
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                RefreshFiles();
                LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Nuevo archivo detectado");
            });
        };

        LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Modo vigilancia activado");
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Modo vigilancia desactivado");
    }
}
