using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Bank;

namespace SalesSystem.DesktopPWF.Views.Bank;

public partial class BankListView : UserControl
{
    public BankListView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is BankListViewModel vm)
        {
            _ = vm.LoadBanksAsync();
        }
    }
}
