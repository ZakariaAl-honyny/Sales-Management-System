using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Accounting;

namespace SalesSystem.DesktopPWF.Views.Accounting;

/// <summary>
/// Interaction logic for ReceiptVoucherEditorView.xaml
/// Editor for creating and editing receipt vouchers (سندات قبض).
/// </summary>
public partial class ReceiptVoucherEditorView : UserControl
{
    public ReceiptVoucherEditorView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext == null)
                DataContext = App.GetService<ReceiptVoucherEditorViewModel>();
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is ReceiptVoucherEditorViewModel vm)
                vm.Cleanup();
        };
    }
}
