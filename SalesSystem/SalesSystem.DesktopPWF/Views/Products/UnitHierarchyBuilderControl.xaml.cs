using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.Views.Products;

public partial class UnitHierarchyBuilderControl : UserControl
{
    public UnitHierarchyBuilderControl()
    {
        InitializeComponent();
    }

    public UnitHierarchyBuilderControl(ProductUnitBuilderViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}