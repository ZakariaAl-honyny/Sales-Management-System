using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Users;

namespace SalesSystem.DesktopPWF.Views.Users;

public partial class UserEditorView : Window
{
    public UserEditorView()
    {
        InitializeComponent();
    }

    public UserEditorView(UserEditorViewModel viewModel) : this()
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

