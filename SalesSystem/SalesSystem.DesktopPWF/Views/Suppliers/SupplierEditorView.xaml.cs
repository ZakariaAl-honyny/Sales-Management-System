using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Suppliers;

/// <summary>
/// Interaction logic for SupplierEditorView.xaml
/// </summary>
public partial class SupplierEditorView : Window
{
    public SupplierEditorView()
    {
        InitializeComponent();
    }

    public SupplierEditorView(ViewModels.Suppliers.SupplierEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close();
    }
}

