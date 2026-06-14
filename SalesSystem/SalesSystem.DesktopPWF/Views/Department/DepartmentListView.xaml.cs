using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Department;

namespace SalesSystem.DesktopPWF.Views.Department;

public partial class DepartmentListView : UserControl
{
    public DepartmentListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DepartmentListViewModel vm)
        {
            _ = vm.LoadDepartmentsAsync();
        }
    }
}
