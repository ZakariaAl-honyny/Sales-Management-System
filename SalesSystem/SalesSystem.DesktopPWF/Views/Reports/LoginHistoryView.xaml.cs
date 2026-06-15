using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class LoginHistoryView : UserControl
{
    public LoginHistoryView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.LoginHistoryViewModel>();
    }
}
