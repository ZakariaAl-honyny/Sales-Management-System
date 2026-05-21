using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Categories;

namespace SalesSystem.DesktopPWF.Views.Categories;

public partial class CategoriesListView : Page
{
    public CategoriesListView()
    {
        InitializeComponent();
        var vm = new CategoryListViewModel();
        DataContext = vm;
        Loaded += CategoriesListView_Loaded;
        Unloaded += (s, e) => vm.Cleanup();
    }

    private async void CategoriesListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CategoryListViewModel vm)
        {
            await vm.LoadCategoriesAsync();
        }
    }
}

