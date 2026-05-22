using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Units;

namespace SalesSystem.DesktopPWF.Views.Units;

public partial class UnitEditorView : Window
{
    public UnitEditorView()
    {
        InitializeComponent();
    }

    public UnitEditorView(UnitEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close();
        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
            });
        };
    }
}

