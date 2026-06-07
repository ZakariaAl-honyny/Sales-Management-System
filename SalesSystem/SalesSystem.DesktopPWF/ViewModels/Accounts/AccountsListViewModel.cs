using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Accounts;

public class AccountsListViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    // Store full tree data for unfiltered rebuilding and search filtering
    private List<AccountTreeNodeDto> _allTreeData = new();

    public AccountsListViewModel()
    {
        _accountService = App.GetService<IAccountApiService>();
        _dialogService = App.GetService<IDialogService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        _eventBus = App.GetService<IEventBus>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        _eventBus.Subscribe<AccountChangedMessage>(OnAccountChanged);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAccountsOperationAsync)));
        AddCommand = new RelayCommand(AddAccount);
        EditSelectedAccountCommand = new RelayCommand(EditSelectedAccount);
        DeleteSelectedAccountCommand = new AsyncRelayCommand(DeleteSelectedAccountAsync);
        ToggleViewCommand = new RelayCommand(ToggleView);
        SearchCommand = new RelayCommand(Search);
    }

    // ── Observable Properties ──

    private bool _isTreeView = true;
    public bool IsTreeView
    {
        get => _isTreeView;
        set => SetProperty(ref _isTreeView, value);
    }

    private ObservableCollection<AccountTreeNodeDto> _treeItems = new();
    public ObservableCollection<AccountTreeNodeDto> TreeItems
    {
        get => _treeItems;
        set => SetProperty(ref _treeItems, value);
    }

    private ObservableCollection<AccountDto> _flatItems = new();
    public ObservableCollection<AccountDto> FlatItems
    {
        get => _flatItems;
        set => SetProperty(ref _flatItems, value);
    }

    private AccountTreeNodeDto? _selectedNode;
    public AccountTreeNodeDto? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                OnPropertyChanged(nameof(CanEditOrDelete));
            }
        }
    }

    private AccountDto? _selectedItem;
    public AccountDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(CanEditOrDelete));
            }
        }
    }

    public bool CanEditOrDelete => (IsTreeView && SelectedNode != null) ||
                                    (!IsTreeView && SelectedItem != null);

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // Auto-search on typing
                _ = ExecuteAsync(LoadAccountsOperationAsync);
            }
        }
    }

    private string _filterType = "0"; // "0" = All
    public string FilterType
    {
        get => _filterType;
        set
        {
            if (SetProperty(ref _filterType, value))
            {
                _ = ExecuteAsync(LoadAccountsOperationAsync);
            }
        }
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // ── AccountType filter list ──

    public List<KeyValuePair<string, string>> AccountTypeFilters { get; } = new()
    {
        new("0", "جميع الأنواع"),
        new("1", "أصل"),
        new("2", "خصم"),
        new("3", "حق ملكية"),
        new("4", "إيراد"),
        new("5", "مصروف")
    };

    // ── Commands ──

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditSelectedAccountCommand { get; private set; } = null!;
    public ICommand DeleteSelectedAccountCommand { get; private set; } = null!;
    public ICommand ToggleViewCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    // ── Operations ──

    private async Task LoadAccountsOperationAsync()
    {
        ErrorMessage = null;

        if (IsTreeView)
        {
            var result = await _accountService.GetTreeAsync();
            if (result.IsSuccess && result.Value != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Store full tree for unfiltered rebuilding
                    _allTreeData = result.Value;

                    var nodes = result.Value;

                    // Apply search filter to tree
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var search = SearchText.Trim();
                        var filtered = FilterTreeNodes(nodes, search);
                        // FilterTreeNodes returns only matching + their ancestors
                        TreeItems.Clear();
                        foreach (var node in filtered)
                            TreeItems.Add(node);
                    }
                    else
                    {
                        TreeItems.Clear();
                        foreach (var node in nodes)
                            TreeItems.Add(node);
                    }

                    IsEmpty = TreeItems.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل دليل الحسابات", "LoadAccounts");
                IsEmpty = TreeItems.Count == 0;
            }
        }
        else
        {
            var result = await _accountService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FlatItems.Clear();
                    var items = result.Value.AsEnumerable();

                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var search = SearchText.Trim();
                        items = items.Where(a =>
                            a.NameAr.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            a.AccountCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (a.NameEn?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
                    }

                    // Apply type filter
                    if (FilterType != "0" && byte.TryParse(FilterType, out var type))
                    {
                        items = items.Where(a => a.AccountType == type);
                    }

                    // Order by AccountCode
                    foreach (var item in items.OrderBy(x => x.AccountCode))
                        FlatItems.Add(item);

                    IsEmpty = FlatItems.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل دليل الحسابات", "LoadAccounts");
                IsEmpty = FlatItems.Count == 0;
            }
        }
    }

    /// <summary>
    /// Recursively filter tree nodes by search term.
    /// A node is included if it matches OR any of its descendants match.
    /// </summary>
    private static List<AccountTreeNodeDto> FilterTreeNodes(List<AccountTreeNodeDto> nodes, string search)
    {
        var result = new List<AccountTreeNodeDto>();
        foreach (var node in nodes)
        {
            var nodeMatches = node.NameAr.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                              node.AccountCode.Contains(search, StringComparison.OrdinalIgnoreCase);

            var filteredChildren = node.Children?.Count > 0
                ? FilterTreeNodes(node.Children, search)
                : new List<AccountTreeNodeDto>();

            if (nodeMatches || filteredChildren.Count > 0)
            {
                result.Add(new AccountTreeNodeDto(
                    node.Id, node.AccountCode, node.NameAr, node.AccountType,
                    node.Level, node.ColorCode, node.AllowTransactions,
                    node.OpeningBalance, node.Explanation, filteredChildren));
            }
        }
        return result;
    }

    private void AddAccount()
    {
        int? parentId = IsTreeView ? SelectedNode?.Id : SelectedItem?.Id;
        var editorVm = new AccountEditorViewModel(_accountService, _dialogService, _toastService, parentId);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة حساب جديد",
            OnClosed = (vm) =>
            {
                if (vm is AccountEditorViewModel editor && editor.IsSaved)
                {
                    _eventBus.Publish(new AccountChangedMessage(0));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadAccountsOperationAsync));
                }
            }
        });
    }

    private void EditSelectedAccount()
    {
        int? accountId = IsTreeView ? SelectedNode?.Id : SelectedItem?.Id;
        if (accountId == null) return;

        var editorVm = new AccountEditorViewModel(
            _accountService, _dialogService, _toastService, null, accountId.Value);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل حساب",
            OnClosed = (vm) =>
            {
                if (vm is AccountEditorViewModel editor && editor.IsSaved)
                {
                    _eventBus.Publish(new AccountChangedMessage(accountId.Value));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadAccountsOperationAsync));
                }
            }
        });
    }

    private async Task DeleteSelectedAccountAsync()
    {
        int? accountId = IsTreeView ? SelectedNode?.Id : SelectedItem?.Id;
        if (accountId == null) return;

        string accountName = IsTreeView ? SelectedNode?.NameAr ?? "" : SelectedItem?.NameAr ?? "";

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الحساب: {accountName}");
        if (strategy == DeleteStrategy.Cancel) return;

        var id = accountId.Value;
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            if (strategy == DeleteStrategy.Deactivate)
            {
                var result = await _accountService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    _toastService.ShowSuccess("تم إلغاء تنشيط الحساب بنجاح");
                    _eventBus.Publish(new AccountChangedMessage(id));
                    await LoadAccountsOperationAsync();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط الحساب", "DeleteAccount");
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var result = await _accountService.PermanentDeleteAsync(id);
                if (result.IsSuccess)
                {
                    _toastService.ShowSuccess("تم حذف الحساب نهائياً");
                    _eventBus.Publish(new AccountChangedMessage(id));
                    await LoadAccountsOperationAsync();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف الحساب", "DeleteAccount");
                }
            }
        });
    }

    private void ToggleView()
    {
        IsTreeView = !IsTreeView;
        _ = ExecuteAsync(LoadAccountsOperationAsync);
    }

    private void Search()
    {
        _ = ExecuteAsync(LoadAccountsOperationAsync);
    }

    private void OnAccountChanged(AccountChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadAccountsOperationAsync));
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<AccountChangedMessage>(OnAccountChanged);
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
