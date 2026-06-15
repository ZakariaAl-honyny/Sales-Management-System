using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class PurchasesByProductView : UserControl
{
    public PurchasesByProductView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.PurchasesByProductViewModel>();
    }
}
