using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class SalesByCustomerView : UserControl
{
    public SalesByCustomerView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.SalesByCustomerViewModel>();
    }
}
