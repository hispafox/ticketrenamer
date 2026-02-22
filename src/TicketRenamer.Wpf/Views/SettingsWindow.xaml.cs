using System.Windows;
using TicketRenamer.Wpf.ViewModels;

namespace TicketRenamer.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.Saved)
        {
            DialogResult = true;
            Close();
        }
    }
}
