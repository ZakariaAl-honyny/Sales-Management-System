using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Customers;

namespace SalesSystem.DesktopPWF.Views.Customers;

public partial class CustomerGroupsListView : UserControl
{
    public CustomerGroupsListView()
    {
        InitializeComponent();
    }

    public CustomerGroupsListView(CustomerGroupListViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
