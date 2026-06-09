using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class PurchasesBySupplierView : UserControl
{
    public PurchasesBySupplierView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.PurchasesBySupplierViewModel>();
    }
}
