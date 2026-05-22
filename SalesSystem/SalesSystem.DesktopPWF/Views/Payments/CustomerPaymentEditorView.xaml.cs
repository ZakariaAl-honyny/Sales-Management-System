using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Payments;

namespace SalesSystem.DesktopPWF.Views.Payments;

public partial class CustomerPaymentEditorView : Window
{
    public CustomerPaymentEditorView()
    {
        InitializeComponent();
    }

    public CustomerPaymentEditorView(CustomerPaymentEditorViewModel viewModel) : this()
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

