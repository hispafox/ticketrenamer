namespace TicketRenamer.Wpf.Services;

public interface IDialogService
{
    bool ShowSettingsWindow();
    string? BrowseFolder(string description);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
}
