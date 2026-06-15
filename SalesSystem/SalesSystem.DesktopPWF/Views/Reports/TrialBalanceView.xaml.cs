using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class TrialBalanceView : UserControl
{
    public TrialBalanceView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.TrialBalanceViewModel>();
    }
}
