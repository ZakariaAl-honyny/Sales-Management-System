using SalesSystem.DesktopPWF.ViewModels.Sessions;

namespace SalesSystem.DesktopPWF.Views.Sessions;

public partial class UserSessionListView : System.Windows.Controls.UserControl
{
    public UserSessionListView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is UserSessionListViewModel vm)
            {
                await vm.LoadUsersAsync();
                await vm.LoadSessionsAsync();
            }
        };
    }
}
