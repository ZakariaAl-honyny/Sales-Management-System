using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Inventory;

namespace SalesSystem.DesktopPWF.Views.Inventory;

public partial class InventoryTransactionEditorView : Window
{
    public InventoryTransactionEditorView()
    {
        InitializeComponent();
    }

    public InventoryTransactionEditorView(InventoryTransactionEditorViewModel viewModel) : this()
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
