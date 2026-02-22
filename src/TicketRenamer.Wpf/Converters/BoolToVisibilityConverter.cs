using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TicketRenamer.Wpf.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true
        };

        var invert = parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase);
        if (invert) isVisible = !isVisible;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
