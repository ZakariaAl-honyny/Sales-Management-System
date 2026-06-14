using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.PaymentVouchers;

namespace SalesSystem.DesktopPWF.Views.PaymentVouchers;

/// <summary>
/// Interaction logic for PaymentVoucherListView.xaml
/// </summary>
public partial class PaymentVoucherListView : System.Windows.Controls.UserControl
{
    public PaymentVoucherListView()
    {
        InitializeComponent();
    }

    private void VouchersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PaymentVoucherListViewModel vm)
        {
            vm.EditVoucherFromDoubleClick();
        }
    }
}
