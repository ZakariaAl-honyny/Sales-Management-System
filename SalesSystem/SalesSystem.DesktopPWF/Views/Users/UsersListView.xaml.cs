using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Users;

namespace SalesSystem.DesktopPWF.Views.Users;

public partial class UsersListView : UserControl
{
    private UserListViewModel? ViewModel => DataContext as UserListViewModel;

    public UsersListView()
    {
        InitializeComponent();
        var vm = new UserListViewModel();
        DataContext = vm;

        Unloaded += (s, e) => vm.Cleanup();
    }
}

