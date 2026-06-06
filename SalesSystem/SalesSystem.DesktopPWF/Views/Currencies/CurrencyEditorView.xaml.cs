using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Currencies;

namespace SalesSystem.DesktopPWF.Views.Currencies;

public partial class CurrencyEditorView : Window
{
    public CurrencyEditorView()
    {
        InitializeComponent();
    }

    public CurrencyEditorView(CurrencyEditorViewModel viewModel) : this()
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
