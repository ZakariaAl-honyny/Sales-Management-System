using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

/// <summary>
/// Interaction logic for ProductImportView.xaml
/// Excel product import screen with preview and validation.
/// </summary>
public partial class ProductImportView : UserControl
{
    public ProductImportView()
    {
        InitializeComponent();

        // Set DataContext from DI if not already set
        if (DataContext == null)
        {
            DataContext = App.GetService<ProductImportViewModel>();
        }
    }

    public ProductImportView(ProductImportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
