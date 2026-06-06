using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Taxes;

namespace SalesSystem.DesktopPWF.Views.Taxes;

public partial class TaxEditorView : Window
{
    public TaxEditorView()
    {
        InitializeComponent();
    }

    public TaxEditorView(TaxEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close();
        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                (ValidationFocusBehavior.FindFirstInvalid(this) ??
                ValidationFocusBehavior.FindFirstEmptyRequired(this))?.Focus();
            });
        };
    }
}
