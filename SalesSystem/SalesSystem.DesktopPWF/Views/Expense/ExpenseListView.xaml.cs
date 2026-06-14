using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Expense;

namespace SalesSystem.DesktopPWF.Views.Expense;

public partial class ExpenseListView : UserControl
{
    private ExpenseListViewModel? _viewModel;

    public ExpenseListView()
    {
        InitializeComponent();
        _viewModel = new ExpenseListViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel ??= DataContext as ExpenseListViewModel;
        _ = (_viewModel?.LoadExpensesAsync() ?? Task.CompletedTask);
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
