using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Payments;

namespace SalesSystem.DesktopPWF.Views.Payments;

public partial class SupplierPaymentEditorView : Window
{
    public SupplierPaymentEditorView()
    {
        InitializeComponent();
    }

    public SupplierPaymentEditorView(SupplierPaymentEditorViewModel viewModel) : this()
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
}

