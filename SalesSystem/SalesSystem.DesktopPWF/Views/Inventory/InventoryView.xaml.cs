using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Inventory;

namespace SalesSystem.DesktopPWF.Views.Inventory;

/// <summary>
/// Interaction logic for InventoryView.xaml
/// </summary>
public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        DataContext = new InventoryViewModel();
    }

}


