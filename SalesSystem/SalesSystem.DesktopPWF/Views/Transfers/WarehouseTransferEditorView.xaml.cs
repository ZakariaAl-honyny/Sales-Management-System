using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

namespace SalesSystem.DesktopPWF.Views.Transfers;

public partial class WarehouseTransferEditorView : Window
{
    public WarehouseTransferEditorView()
    {
        InitializeComponent();
    }

    public WarehouseTransferEditorView(WarehouseTransferEditorViewModel viewModel) : this()
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

