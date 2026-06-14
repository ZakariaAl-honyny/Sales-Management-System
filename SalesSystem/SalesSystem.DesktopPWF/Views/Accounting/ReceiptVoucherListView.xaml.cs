using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Accounting;

namespace SalesSystem.DesktopPWF.Views.Accounting;

/// <summary>
/// Interaction logic for ReceiptVoucherListView.xaml
/// Displays a list of receipt vouchers (سندات قبض) with add, edit, post, cancel, delete operations.
/// </summary>
public partial class ReceiptVoucherListView : UserControl
{
    public ReceiptVoucherListView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext == null)
                DataContext = App.GetService<ReceiptVoucherListViewModel>();
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is ReceiptVoucherListViewModel vm)
                vm.Cleanup();
        };
    }
}
