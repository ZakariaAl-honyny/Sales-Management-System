using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.Views;

/// <summary>
/// Dashboard View - displays system statistics and overview
/// </summary>
public partial class DashboardView : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView()
    {
        InitializeComponent();

        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        Unloaded += DashboardView_Unloaded;
    }

    private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }
}

