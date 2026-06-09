using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class SalesByProductView : UserControl
{
    public SalesByProductView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.SalesByProductViewModel>();
    }
}
