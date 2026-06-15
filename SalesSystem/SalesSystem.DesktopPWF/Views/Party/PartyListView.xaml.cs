using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Party;

namespace SalesSystem.DesktopPWF.Views.Party;

public partial class PartyListView : UserControl
{
    private PartyListViewModel? _viewModel;

    public PartyListView()
    {
        InitializeComponent();
        _viewModel = new PartyListViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel ??= DataContext as PartyListViewModel;
        _ = (_viewModel?.LoadPartiesAsync() ?? Task.CompletedTask);
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Cleanup();
            _viewModel = null;
        }
    }
}
