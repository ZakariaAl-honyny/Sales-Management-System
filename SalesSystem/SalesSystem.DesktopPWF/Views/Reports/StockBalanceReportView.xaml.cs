using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for StockBalanceReportView.xaml
/// </summary>
public partial class StockBalanceReportView : UserControl
{
    public StockBalanceReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.StockBalanceReportViewModel>();
    }
}
