using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Units;

namespace SalesSystem.DesktopPWF.Views.Units;

public partial class UnitsListView : UserControl
{
    public UnitsListView()
    {
        InitializeComponent();
        var vm = new UnitListViewModel();
        DataContext = vm;
        Loaded += UnitsListView_Loaded;
        Unloaded += (s, e) => vm.Cleanup();
    }

    private async void UnitsListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UnitListViewModel vm)
        {
            await vm.LoadUnitsAsync();
        }
    }
}

