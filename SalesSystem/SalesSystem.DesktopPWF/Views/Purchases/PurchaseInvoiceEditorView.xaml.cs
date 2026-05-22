using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

namespace SalesSystem.DesktopPWF.Views.Purchases;

/// <summary>
/// Interaction logic for PurchaseInvoiceEditorView.xaml
/// </summary>
public partial class PurchaseInvoiceEditorView : Window
{
    public PurchaseInvoiceEditorView()
    {
        InitializeComponent();
    }

    public PurchaseInvoiceEditorView(PurchaseInvoiceEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                Helpers.ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
            });
        };
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        // Skip system keys that shouldn't be part of a barcode
        if (e.Key == System.Windows.Input.Key.LeftShift || e.Key == System.Windows.Input.Key.RightShift || 
            e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl ||
            e.Key == System.Windows.Input.Key.LeftAlt || e.Key == System.Windows.Input.Key.RightAlt ||
            e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Escape)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        if (DataContext is PurchaseInvoiceEditorViewModel viewModel)
        {
            _ = viewModel.HandleBarcodeInput(e.Key);
        }

        base.OnPreviewKeyDown(e);
    }
}
