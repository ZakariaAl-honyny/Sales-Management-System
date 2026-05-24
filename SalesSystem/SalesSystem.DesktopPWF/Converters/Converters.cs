using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SalesSystem.DesktopPWF;

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts boolean to Visibility (true = Collapsed, false = Visible)
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// Converts null to Visibility (null/empty = Collapsed, not null = Visible)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Visibility.Collapsed;

        if (value is string str && string.IsNullOrEmpty(str))
            return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to Visibility (null/empty = Collapsed, not null = Visible)
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to its inverse (true = false, false = true)
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Converts count to Visibility (count > 0 = Visible, count == 0 = Collapsed)
/// Can be inverted with parameter "Inverse"
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = 0;
        
        if (value is int intValue)
            count = intValue;
        else if (value != null && int.TryParse(value.ToString(), out int parsed))
            count = parsed;
        
        bool inverse = parameter?.ToString() == "Inverse";
        
        if (inverse)
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        
        return count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if Quantity <= ReorderLevel
/// </summary>
public class LowStockConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is decimal quantity && values[1] is decimal reorderLevel)
        {
            return quantity <= reorderLevel;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts InvoiceStatus byte to a Brush for status badges
/// </summary>
public class InvoiceStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte status || value is int statusInt && (status = (byte)statusInt) >= 0)
        {
            return (byte)value switch
            {
                1 => System.Windows.Application.Current.Resources["WarningBrush"] ?? System.Windows.Media.Brushes.Orange,  // Draft
                2 => System.Windows.Application.Current.Resources["SuccessBrush"] ?? System.Windows.Media.Brushes.Green,   // Posted
                3 => System.Windows.Application.Current.Resources["ErrorBrush"] ?? System.Windows.Media.Brushes.Red,       // Cancelled
                _ => System.Windows.Media.Brushes.Gray
            };
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts InvoiceStatus byte to its Arabic string representation
/// </summary>
public class InvoiceStatusToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte status || value is int statusInt && (status = (byte)statusInt) >= 0)
        {
            return (byte)value switch
            {
                1 => "مسودة",
                2 => "تم الترحيل",
                3 => "ملغي",
                _ => "غير معروف"
            };
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SaleMode byte to a Brush
/// </summary>
public class SaleModeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte mode || value is int modeInt && (mode = (byte)modeInt) >= 0)
        {
            return mode switch
            {
                1 => System.Windows.Application.Current.Resources["PrimaryBrush"] ?? System.Windows.Media.Brushes.Blue,    // Retail
                2 => System.Windows.Application.Current.Resources["SecondaryBrush"] ?? System.Windows.Media.Brushes.Purple, // Wholesale
                _ => System.Windows.Media.Brushes.Gray
            };
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts List&lt;string&gt; to comma-separated string for display
/// </summary>
public class StringListJoinConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.IList list)
        {
            var items = list.Cast<object>().Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x));
            return string.Join("، ", items);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SaleMode byte to Arabic string
/// </summary>
public class SaleModeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte mode || value is int modeInt && (mode = (byte)modeInt) >= 0)
        {
            return mode switch
            {
                1 => "تجزئة",
                2 => "جملة",
                _ => "غير معروف"
            };
        }
        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
