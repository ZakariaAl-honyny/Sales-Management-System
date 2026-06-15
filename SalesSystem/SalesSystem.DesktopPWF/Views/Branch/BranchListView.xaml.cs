using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Branch;

namespace SalesSystem.DesktopPWF.Views.Branch;

public partial class BranchListView : UserControl
{
    public BranchListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is BranchListViewModel vm)
        {
            _ = vm.LoadBranchesAsync();
        }
    }
}
