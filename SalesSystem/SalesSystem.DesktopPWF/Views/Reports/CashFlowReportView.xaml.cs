using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for CashFlowReportView.xaml
/// </summary>
public partial class CashFlowReportView : UserControl
{
    public CashFlowReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.CashFlowReportViewModel>();
    }
}
