using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Currencies;

namespace SalesSystem.DesktopPWF.Views.Currencies;

public partial class CurrenciesListView : UserControl
{
    public CurrenciesListView()
    {
        InitializeComponent();
        var vm = new CurrenciesListViewModel();
        DataContext = vm;
        Loaded += CurrenciesListView_Loaded;
        Unloaded += (s, e) => vm.Cleanup();
    }

    private async void CurrenciesListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CurrenciesListViewModel vm)
        {
            await vm.LoadCurrenciesAsync();
        }
    }
}
