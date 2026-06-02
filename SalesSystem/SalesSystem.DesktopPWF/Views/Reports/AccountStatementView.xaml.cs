using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for AccountStatementView.xaml
/// </summary>
public partial class AccountStatementView : UserControl
{
    public AccountStatementView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.AccountStatementViewModel>();
    }
}
