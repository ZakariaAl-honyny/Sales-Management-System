using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class ProductUnitsListView : UserControl
{
    private readonly ProductUnitsListViewModel _viewModel;

    public ProductUnitsListView()
    {
        InitializeComponent();

        _viewModel = new ProductUnitsListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
