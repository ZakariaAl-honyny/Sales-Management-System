using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Inventory;

namespace SalesSystem.DesktopPWF.Views.Inventory;

public partial class LowStockView : Page
{
    public LowStockView()
    {
        InitializeComponent();
    }

    public LowStockView(LowStockViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
