using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Employee;

namespace SalesSystem.DesktopPWF.Views.Employee;

public partial class EmployeeListView : UserControl
{
    public EmployeeListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is EmployeeListViewModel vm)
        {
            _ = vm.LoadEmployeesAsync();
        }
    }
}
