using System.Windows;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.Views.Products;

/// <summary>
/// Interaction logic for ProductEditorView.xaml
/// </summary>
public partial class ProductEditorView : Window
{
    public ProductEditorView()
    {
        InitializeComponent();
    }

    public ProductEditorView(ViewModels.Products.ProductEditorViewModel viewModel) : this()
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

