using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels;
using Serilog;

namespace SalesSystem.DesktopPWF.Views.Settings;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        try
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[SettingsView.Constructor] Failed to initialize SettingsView components.");
            throw; // Re-throw to be caught by global handler in App.xaml.cs
        }
    }
}

