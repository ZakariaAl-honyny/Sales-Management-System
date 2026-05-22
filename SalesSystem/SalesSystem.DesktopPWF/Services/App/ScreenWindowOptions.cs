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
}
