using System.Windows;

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
    }
}

