using System.Windows;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.Views.Suppliers;

/// <summary>
/// Interaction logic for SupplierEditorView.xaml
/// </summary>
public partial class SupplierEditorView : Window
{
    public SupplierEditorView()
    {
        InitializeComponent();
    }

    public SupplierEditorView(ViewModels.Suppliers.SupplierEditorViewModel viewModel) : this()
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

