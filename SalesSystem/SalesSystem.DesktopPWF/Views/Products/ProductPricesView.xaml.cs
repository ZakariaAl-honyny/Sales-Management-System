using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class ProductPricesView : UserControl
{
    private readonly ProductPricesListViewModel _viewModel;

    public ProductPricesView()
    {
        InitializeComponent();

        _viewModel = new ProductPricesListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
