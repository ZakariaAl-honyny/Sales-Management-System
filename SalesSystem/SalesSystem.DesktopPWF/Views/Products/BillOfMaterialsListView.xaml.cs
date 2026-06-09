using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class BillOfMaterialsListView : UserControl
{
    private readonly BillOfMaterialsListViewModel _viewModel;

    public BillOfMaterialsListView()
    {
        InitializeComponent();

        _viewModel = new BillOfMaterialsListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
