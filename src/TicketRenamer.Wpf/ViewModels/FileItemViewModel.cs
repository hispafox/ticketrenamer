using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TicketRenamer.Core.Models;

namespace TicketRenamer.Wpf.ViewModels;

public enum FileProcessingState
{
    Pending,
    Processing,
    Completed,
    Failed
}

public partial class FileItemViewModel : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _extension = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private FileProcessingState _state = FileProcessingState.Pending;
    [ObservableProperty] private string? _newFileName;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private ProcessingStatus? _processingStatus;
    [ObservableProperty] private DateTime? _processedAt;

    public static FileItemViewModel FromFile(string filePath)
    {
        var info = new FileInfo(filePath);
        return new FileItemViewModel
        {
            FileName = info.Name,
            FilePath = filePath,
            Extension = info.Extension.ToLowerInvariant(),
            FileSize = info.Length
        };
    }

    public void UpdateFromResult(ProcessingResult result)
    {
        NewFileName = result.NewFileName;
        ErrorMessage = result.ErrorMessage;
        ProcessingStatus = result.Status;
        ProcessedAt = result.ProcessedAt;
        State = result.Status == Core.Models.ProcessingStatus.Success
            ? FileProcessingState.Completed
            : FileProcessingState.Failed;
    }
}
