using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Returns;

namespace SalesSystem.DesktopPWF.Views.Returns;

public partial class SalesReturnEditorView : Window
{
    public SalesReturnEditorView()
    {
        InitializeComponent();
    }

    public SalesReturnEditorView(SalesReturnEditorViewModel viewModel) : this()
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


