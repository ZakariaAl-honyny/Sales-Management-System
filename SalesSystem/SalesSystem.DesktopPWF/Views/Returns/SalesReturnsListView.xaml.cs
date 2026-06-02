using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Returns;

namespace SalesSystem.DesktopPWF.Views.Returns;

/// <summary>
/// Interaction logic for SalesReturnsListView.xaml
/// </summary>
public partial class SalesReturnsListView : UserControl
{
    private SalesReturnListViewModel? _viewModel;

    public SalesReturnsListView()
    {
        InitializeComponent();
        _viewModel = new SalesReturnListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

}

