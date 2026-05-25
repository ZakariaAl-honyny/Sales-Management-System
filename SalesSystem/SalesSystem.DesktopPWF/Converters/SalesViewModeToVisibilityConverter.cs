using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SalesSystem.DesktopPWF;

/// <summary>
/// Converts <see cref="SalesInvoiceEditorViewModel.SalesViewMode"/> to <see cref="Visibility"/>.
/// Used to toggle visibility between Standard and Touch UI views based on the current view mode.
/// </summary>
/// <remarks>
/// Pass the mode to match as the converter parameter: <c>"Standard"</c> or <c>"Touch"</c>.
/// Returns <see cref="Visibility.Visible"/> when the current view mode matches the parameter,
/// <see cref="Visibility.Collapsed"/> otherwise.
/// </remarks>
public class SalesViewModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && parameter is string modeString)
        {
            return value.ToString()!.Equals(modeString, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
