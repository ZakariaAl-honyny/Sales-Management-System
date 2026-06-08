using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.InventoryOperations;

namespace SalesSystem.DesktopPWF.Views.InventoryOperations;

/// <summary>
/// Interaction logic for InventoryOperationListView.xaml
/// </summary>
public partial class InventoryOperationListView : UserControl
{
    public InventoryOperationListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InventoryOperationListViewModel vm)
        {
            await vm.LoadWarehousesAsync();
        }
    }
}
