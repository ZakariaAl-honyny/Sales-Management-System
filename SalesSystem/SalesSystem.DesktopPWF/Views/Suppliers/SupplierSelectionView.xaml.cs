using SalesSystem.DesktopPWF.ViewModels.Suppliers;
using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Suppliers;

public partial class SupplierSelectionView : Window
{
    public SupplierSelectionView()
    {
        InitializeComponent();
    }

    public SupplierSelectionView(SupplierSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
