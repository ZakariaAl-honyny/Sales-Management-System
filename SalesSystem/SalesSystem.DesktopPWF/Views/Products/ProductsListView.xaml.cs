using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

/// <summary>
/// Interaction logic for ProductsListView.xaml
/// </summary>
public partial class ProductsListView : UserControl
{
    private readonly ProductListViewModel _viewModel;

    public ProductsListView()
    {
        InitializeComponent();

        _viewModel = new ProductListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

    private void ProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is ProductDto)
        {
            _viewModel.EditProductFromDoubleClick();
        }
    }
}

