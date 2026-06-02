using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Returns;

namespace SalesSystem.DesktopPWF.Views.Returns;

/// <summary>
/// Interaction logic for PurchaseReturnsListView.xaml
/// </summary>
public partial class PurchaseReturnsListView : UserControl
{
    private PurchaseReturnListViewModel? _viewModel;

    public PurchaseReturnsListView()
    {
        InitializeComponent();
        _viewModel = new PurchaseReturnListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

}

