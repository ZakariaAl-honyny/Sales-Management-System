using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

/// <summary>
/// Interaction logic for ProductCategoryEditorView.xaml
/// </summary>
public partial class ProductCategoryEditorView : Window
{
    private readonly ProductCategoryEditorViewModel _viewModel;

    public ProductCategoryEditorView()
    {
        InitializeComponent();

        _viewModel = new ProductCategoryEditorViewModel();
        DataContext = _viewModel;

        // Close window when ViewModel requests it
        _viewModel.CloseRequested += () =>
        {
            Close();
        };

        Loaded += (s, e) =>
        {
            // Focus the name field on load
            TxtCategoryName?.Focus();
        };
    }

    public ProductCategoryEditorView(ProductCategoryEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        // Close window when ViewModel requests it (no DialogResult — opened non-modally via ScreenWindowService)
        _viewModel.CloseRequested += () =>
        {
            Close();
        };

        Loaded += (s, e) =>
        {
            TxtCategoryName?.Focus();
        };
    }
}
