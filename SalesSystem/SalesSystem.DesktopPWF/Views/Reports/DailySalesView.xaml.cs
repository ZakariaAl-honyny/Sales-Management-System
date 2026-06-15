using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class DailySalesView : UserControl
{
    public DailySalesView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.DailySalesViewModel>();
    }
}
