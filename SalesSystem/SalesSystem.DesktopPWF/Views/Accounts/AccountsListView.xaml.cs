using System.Windows;
using System.Windows.Controls;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.ViewModels.Accounts;

namespace SalesSystem.DesktopPWF.Views.Accounts;

public partial class AccountsListView : UserControl
{
    public AccountsListView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext is AccountsListViewModel vm)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(AccountsListViewModel.IsTreeView))
                    {
                        UpdateToggleButtonText(vm.IsTreeView);
                    }
                };
                UpdateToggleButtonText(vm.IsTreeView);
            }
        };
    }

    private void UpdateToggleButtonText(bool isTreeView)
    {
        if (ToggleViewText != null)
        {
            ToggleViewText.Text = isTreeView ? "📋 عرض جدولي" : "🌳 عرض شجري";
        }
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AccountsListViewModel vm && e.NewValue is AccountTreeNodeDto node)
        {
            vm.SelectedNode = node;
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is AccountsListViewModel vm && vm.EditSelectedAccountCommand.CanExecute(null))
        {
            vm.EditSelectedAccountCommand.Execute(null);
        }
    }

    private void TreeViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is AccountsListViewModel vm && vm.EditSelectedAccountCommand.CanExecute(null))
        {
            vm.EditSelectedAccountCommand.Execute(null);
        }
    }
}
