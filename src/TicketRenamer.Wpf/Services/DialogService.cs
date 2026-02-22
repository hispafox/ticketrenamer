using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using TicketRenamer.Wpf.ViewModels;
using TicketRenamer.Wpf.Views;

namespace TicketRenamer.Wpf.Services;

public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool ShowSettingsWindow()
    {
        var vm = _serviceProvider.GetRequiredService<SettingsViewModel>();
        vm.LoadFromSettings();
        var window = new SettingsWindow { DataContext = vm, Owner = Application.Current.MainWindow };
        return window.ShowDialog() == true;
    }

    public string? BrowseFolder(string description)
    {
        var dialog = new OpenFolderDialog { Title = description };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public void ShowError(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
