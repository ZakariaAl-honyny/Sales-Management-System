using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;

namespace SalesSystem.DesktopPWF.Views.CustomerReceipt;

/// <summary>
/// Interaction logic for CustomerReceiptListView.xaml
/// </summary>
public partial class CustomerReceiptListView : System.Windows.Controls.UserControl
{
    public CustomerReceiptListView()
    {
        InitializeComponent();
    }

    private void ReceiptsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CustomerReceiptListViewModel vm)
        {
            vm.EditReceiptFromDoubleClick();
        }
    }
}
