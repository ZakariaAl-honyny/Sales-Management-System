using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SalesSystem.DesktopPWF;

public class ImagePathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // Return UnsetValue to leave the property unchanged when image can't be loaded
                return DependencyProperty.UnsetValue;
            }
        }
        // Return UnsetValue for null/empty to avoid ImageSourceConverter errors
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
