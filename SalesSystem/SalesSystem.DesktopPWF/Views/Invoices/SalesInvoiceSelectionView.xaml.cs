using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Invoices;

namespace SalesSystem.DesktopPWF.Views.Invoices;

public partial class SalesInvoiceSelectionView : Window
{
    public SalesInvoiceSelectionView()
    {
        InitializeComponent();
    }

    public SalesInvoiceSelectionView(SalesInvoiceSelectionViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
