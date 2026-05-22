using System.Windows;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.Views.Customers;

/// <summary>
/// Interaction logic for CustomerEditorView.xaml
/// </summary>
public partial class CustomerEditorView : Window
{
    public CustomerEditorView()
    {
        InitializeComponent();
    }

    public CustomerEditorView(ViewModels.Customers.CustomerEditorViewModel viewModel) : this()
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

