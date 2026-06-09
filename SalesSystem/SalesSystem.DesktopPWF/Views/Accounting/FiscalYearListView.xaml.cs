using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Accounting;

namespace SalesSystem.DesktopPWF.Views.Accounting;

/// <summary>
/// Interaction logic for FiscalYearListView.xaml
/// Displays a list of fiscal years with Create, Open, and Close operations.
/// </summary>
public partial class FiscalYearListView : UserControl
{
    public FiscalYearListView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext == null)
                DataContext = App.GetService<FiscalYearListViewModel>();
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is FiscalYearListViewModel vm)
                vm.Cleanup();
        };
    }
}
