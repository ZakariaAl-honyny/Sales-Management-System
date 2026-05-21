using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

#if DEBUG
        // DEBUG (E2E test mode): Standard window style for UIA3 accessibility
        WindowStyle = WindowStyle.SingleBorderWindow;
        AllowsTransparency = false;
        Background = System.Windows.Media.Brushes.White;
#else
        // RELEASE (production): Custom chrome with transparency for modern look
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
#endif

        // Create ViewModel with services from DI
        var authService = App.GetService<IAuthApiService>();
        var sessionService = App.GetService<ISessionService>();

        DataContext = new LoginWindowViewModel(authService, sessionService);

        // Enable window dragging
        MouseDown += (s, e) =>
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
