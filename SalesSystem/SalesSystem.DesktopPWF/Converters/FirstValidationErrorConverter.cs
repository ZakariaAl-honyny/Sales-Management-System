using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SalesSystem.DesktopPWF;

/// <summary>
/// Safely extracts the first validation error's ErrorContent from a
/// ReadOnlyObservableCollection{ValidationError}. Returns
/// DependencyProperty.UnsetValue (no ToolTip) when the collection is null
/// or empty — avoiding the WPF warning that the indexer [0] causes on an
/// empty collection.
/// </summary>
public class FirstValidationErrorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ReadOnlyObservableCollection<ValidationError> errors && errors.Count > 0)
        {
            return errors[0].ErrorContent ?? "";
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
