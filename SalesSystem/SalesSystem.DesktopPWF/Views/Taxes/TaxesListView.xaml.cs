using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Taxes;

namespace SalesSystem.DesktopPWF.Views.Taxes;

public partial class TaxesListView : UserControl
{
    public TaxesListView()
    {
        InitializeComponent();
        var vm = new TaxesListViewModel();
        DataContext = vm;
        Loaded += TaxesListView_Loaded;
        Unloaded += (s, e) => vm.Cleanup();
    }

    private async void TaxesListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaxesListViewModel vm)
        {
            await vm.LoadTaxesAsync();
        }
    }
}
