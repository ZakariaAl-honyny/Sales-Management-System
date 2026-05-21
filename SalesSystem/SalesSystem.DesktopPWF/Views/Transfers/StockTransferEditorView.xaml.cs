using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

namespace SalesSystem.DesktopPWF.Views.Transfers;

public partial class StockTransferEditorView : Window
{
    public StockTransferEditorView()
    {
        InitializeComponent();
    }

    public StockTransferEditorView(StockTransferEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
