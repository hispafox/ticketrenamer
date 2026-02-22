using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TicketRenamer.Wpf.ViewModels;

namespace TicketRenamer.Wpf.Converters;

public sealed class StatusToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Gray = new(Colors.Gray);
    private static readonly SolidColorBrush Blue = new(Color.FromRgb(0, 120, 212));
    private static readonly SolidColorBrush Green = new(Color.FromRgb(16, 124, 16));
    private static readonly SolidColorBrush Red = new(Color.FromRgb(196, 43, 28));

    static StatusToColorConverter()
    {
        Gray.Freeze();
        Blue.Freeze();
        Green.Freeze();
        Red.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is FileProcessingState state ? state switch
        {
            FileProcessingState.Pending => Gray,
            FileProcessingState.Processing => Blue,
            FileProcessingState.Completed => Green,
            FileProcessingState.Failed => Red,
            _ => Gray
        } : Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
