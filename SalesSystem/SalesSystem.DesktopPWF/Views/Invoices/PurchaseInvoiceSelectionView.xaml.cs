using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Invoices;

namespace SalesSystem.DesktopPWF.Views.Invoices;

public partial class PurchaseInvoiceSelectionView : Window
{
    public PurchaseInvoiceSelectionView()
    {
        InitializeComponent();
    }

    public PurchaseInvoiceSelectionView(PurchaseInvoiceSelectionViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
