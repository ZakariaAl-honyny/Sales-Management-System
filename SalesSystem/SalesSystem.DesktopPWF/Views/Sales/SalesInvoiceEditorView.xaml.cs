using System;
using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Sales;

namespace SalesSystem.DesktopPWF.Views.Sales;

/// <summary>
/// Interaction logic for SalesInvoiceEditorView.xaml
/// </summary>
public partial class SalesInvoiceEditorView : Window
{
    public SalesInvoiceEditorView()
    {
        InitializeComponent();
    }

    public SalesInvoiceEditorView(SalesInvoiceEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        IsReadOnly = viewModel.IsReadOnly;

        viewModel.CloseRequested += () => Dispatcher.InvokeAsync(() => Close());
        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                (Helpers.ValidationFocusBehavior.FindFirstInvalid(this) ??
                Helpers.ValidationFocusBehavior.FindFirstEmptyRequired(this))?.Focus();
            });
        };
    }       

    public bool IsReadOnly
    {
        get => !IsEnabled;
        set
        {
            if (value)
            {
                IsEnabled = false;
                Title = "ط¹ط±ط¶ ظپط§طھظˆط±ط© ط¨ظٹط¹";
            }
        }
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

        if (DataContext is SalesInvoiceEditorViewModel viewModel)
        {
            // Pass the key to the ViewModel/Service
            _ = viewModel.HandleBarcodeInput(e.Key);
            
            // If it was Enter and we have a barcode, Handled will be set inside HandleBarcodeInput
            // Wait, we don't know if it was handled. 
            // Actually, if we are in the Barcode TextBox, we WANT Enter to trigger the command anyway.
            // If we are NOT in the TextBox, we might want to prevent Enter from doing other things if it was a scan.
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }
}

