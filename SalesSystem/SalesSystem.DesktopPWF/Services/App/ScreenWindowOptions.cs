using System.Windows;

namespace SalesSystem.DesktopPWF.Services.App;

public class ScreenWindowOptions
{
    public string? Title { get; set; }
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 650;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool IsModal { get; set; } = false;
    public WindowStartupLocation StartupLocation { get; set; } = WindowStartupLocation.CenterOwner;
    public bool CanResize { get; set; } = true;
    public WindowStyle Style { get; set; } = WindowStyle.SingleBorderWindow;
    public Action<object?>? OnClosed { get; set; }

    /// <summary>
    /// When true, the window cannot be closed by the user (X button, Alt+F4, system menu).
    /// The window can only close when the ViewModel sets DialogResult = true and calls RequestClose().
    /// Used for mandatory flows like forced password change on first login.
    /// </summary>
    public bool PreventClose { get; set; } = false;
}
