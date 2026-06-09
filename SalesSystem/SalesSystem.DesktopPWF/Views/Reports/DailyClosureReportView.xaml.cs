using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class DailyClosureReportView : UserControl
{
    public DailyClosureReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.DailyClosureReportViewModel>();
    }
}
