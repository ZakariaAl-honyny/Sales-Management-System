using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

/// <summary>
/// Interaction logic for ProductCategoriesListView.xaml
/// </summary>
public partial class ProductCategoriesListView : UserControl
{
    private readonly ProductCategoriesListViewModel _viewModel;

    public ProductCategoriesListView()
    {
        InitializeComponent();

        _viewModel = new ProductCategoriesListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

    private void CategoriesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is ProductCategoryDto)
        {
            _viewModel.EditCategoryFromDoubleClick();
        }
    }
}
