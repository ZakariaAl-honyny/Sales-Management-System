using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class UserActivityView : UserControl
{
    public UserActivityView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.UserActivityViewModel>();
    }
}
