using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

namespace SalesSystem.DesktopPWF.Views.Transfers;

public partial class StockTransferEditorView : Window
{
    public StockTransferEditorView()
    {
        InitializeComponent();
    }

    public StockTransferEditorView(StockTransferEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;

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
}

