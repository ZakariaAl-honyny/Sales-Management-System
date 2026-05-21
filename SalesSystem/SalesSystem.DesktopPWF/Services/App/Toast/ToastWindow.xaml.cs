using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.Services.App.Toast;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer;

    public ToastWindow(string message, ToastType type, TimeSpan duration)
    {
        InitializeComponent();

        var (bg, iconChar, iconColor) = type switch
        {
            ToastType.Success => (ColorFromHex("#D4EDDA"), "✓", ColorFromHex("#155724")),
            ToastType.Error => (ColorFromHex("#F8D7DA"), "✕", ColorFromHex("#721C24")),
            ToastType.Info => (ColorFromHex("#D1ECF1"), "ℹ", ColorFromHex("#0C5460")),
            _ => (ColorFromHex("#D1ECF1"), "ℹ", ColorFromHex("#0C5460"))
        };

        BackBorder.Background = new SolidColorBrush(bg);
        IconText.Text = iconChar;
        IconText.Foreground = new SolidColorBrush(iconColor);
        MessageText.Text = message;
        MessageText.Foreground = new SolidColorBrush(iconColor);

        Left = SystemParameters.PrimaryScreenWidth - ActualWidth - 20;
        Top = 20;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { BeginTime = duration };
        fade.Completed += (s, e) => Close();
        BeginAnimation(OpacityProperty, fade);

        _timer = new DispatcherTimer { Interval = duration.Add(TimeSpan.FromMilliseconds(300)) };
        _timer.Tick += (s, e) =>
        {
            _timer.Stop();
            Close();
        };
        _timer.Start();
    }

    public void CloseToast()
    {
        var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(200));
        fade.Completed += (s, e) => Close();
        BeginAnimation(OpacityProperty, fade);
        _timer.Stop();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseToast();

    private static System.Windows.Media.Color ColorFromHex(string hex) => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
}