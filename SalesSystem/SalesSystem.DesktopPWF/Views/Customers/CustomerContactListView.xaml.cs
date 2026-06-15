using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Customers;

namespace SalesSystem.DesktopPWF.Views.Customers;

public partial class CustomerContactListView : UserControl
{
    public CustomerContactListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CustomerContactListViewModel vm)
        {
            _ = vm.LoadContactsAsync();
        }
    }
}
