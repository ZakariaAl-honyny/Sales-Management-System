using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class SalesByCategoryView : UserControl
{
    public SalesByCategoryView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.SalesByCategoryViewModel>();
    }
}
