using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class BalanceSheetView : UserControl
{
    public BalanceSheetView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.BalanceSheetViewModel>();
    }
}
