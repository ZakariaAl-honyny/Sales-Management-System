using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Returns;

namespace SalesSystem.DesktopPWF.Views.Returns;

public partial class PurchaseReturnEditorView : Window
{
    public PurchaseReturnEditorView()
    {
        InitializeComponent();
    }

    public PurchaseReturnEditorView(PurchaseReturnEditorViewModel viewModel) : this()
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
