using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;

namespace SalesSystem.DesktopPWF.Views.Suppliers;

public partial class SupplierContactListView : UserControl
{
    public SupplierContactListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SupplierContactListViewModel vm)
        {
            _ = vm.LoadContactsAsync();
        }
    }
}
