using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class ProductImagesView : UserControl
{
    private readonly ProductImagesViewModel _viewModel;

    public ProductImagesView()
    {
        InitializeComponent();

        _viewModel = new ProductImagesViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
