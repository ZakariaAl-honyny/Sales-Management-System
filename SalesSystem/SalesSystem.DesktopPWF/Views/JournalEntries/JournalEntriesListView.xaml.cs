using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.JournalEntries;

namespace SalesSystem.DesktopPWF.Views.JournalEntries;

/// <summary>
/// Interaction logic for JournalEntriesListView.xaml
/// Displays a list of journal entries with status indicators.
/// </summary>
public partial class JournalEntriesListView : UserControl
{
    public JournalEntriesListView()
    {
        InitializeComponent();

        // Set DataContext from DI
        Loaded += JournalEntriesListView_Loaded;
        Unloaded += (s, e) =>
        {
            if (DataContext is JournalEntriesListViewModel vm)
                vm.Cleanup();
        };
    }

    private async void JournalEntriesListView_Loaded(object sender, RoutedEventArgs e)
    {
        // Lazy-load the ViewModel from DI to avoid design-time issues
        if (DataContext == null)
        {
            DataContext = App.GetService<JournalEntriesListViewModel>();
        }

        if (DataContext is JournalEntriesListViewModel vm)
        {
            // Execute the refresh command to load data
            vm.RefreshCommand.Execute(null);
        }
    }
}
