using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Customers;

namespace SalesSystem.DesktopPWF.Views.Customers;

public partial class CustomerGroupEditorView : Window
{
    public CustomerGroupEditorView()
    {
        InitializeComponent();
    }

    public CustomerGroupEditorView(CustomerGroupEditorViewModel viewModel) : this()
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
