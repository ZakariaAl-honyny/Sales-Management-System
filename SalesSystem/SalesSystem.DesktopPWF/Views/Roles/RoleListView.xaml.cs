using SalesSystem.DesktopPWF.ViewModels.Roles;

namespace SalesSystem.DesktopPWF.Views.Roles;

public partial class RoleListView : System.Windows.Controls.UserControl
{
    public RoleListView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is RoleListViewModel vm)
            {
                await vm.LoadRolesAsync();
            }
        };
    }
}
