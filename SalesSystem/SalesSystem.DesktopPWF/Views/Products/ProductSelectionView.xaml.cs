using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class ProductSelectionView : Window
{
    public ProductSelectionView()
    {
        InitializeComponent();
        
        // Auto-focus SearchBox when window loads
        Loaded += (s, e) => SearchBox.Focus();

        // Keyboard usability: pressing Down arrow in search box moves focus to DataGrid
        SearchBox.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Down && ProductsDataGrid.Items.Count > 0)
            {
                ProductsDataGrid.Focus();
                
                // Select the first item if nothing is selected
                if (ProductsDataGrid.SelectedItem == null)
                {
                    ProductsDataGrid.SelectedIndex = 0;
                }
                
                // Focus the row container to make it visible/active
                var row = ProductsDataGrid.ItemContainerGenerator.ContainerFromIndex(ProductsDataGrid.SelectedIndex) as UIElement;
                row?.Focus();
                
                e.Handled = true;
            }
        };
    }

    public ProductSelectionView(ProductSelectionViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
