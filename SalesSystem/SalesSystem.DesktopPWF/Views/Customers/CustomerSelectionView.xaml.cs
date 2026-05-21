using SalesSystem.DesktopPWF.ViewModels.Customers;
using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Customers;

public partial class CustomerSelectionView : Window
{
    public CustomerSelectionView()
    {
        InitializeComponent();
    }

    public CustomerSelectionView(CustomerSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
