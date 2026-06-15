using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class CashBoxSummaryView : UserControl
{
    public CashBoxSummaryView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.CashBoxSummaryViewModel>();
    }
}
