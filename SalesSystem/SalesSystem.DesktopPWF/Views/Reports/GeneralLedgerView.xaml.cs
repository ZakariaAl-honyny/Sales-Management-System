using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class GeneralLedgerView : UserControl
{
    public GeneralLedgerView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.GeneralLedgerViewModel>();
    }
}
