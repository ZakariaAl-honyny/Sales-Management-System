using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Inventory;

/// <summary>
/// Dedicated ViewModel for browsing and filtering inventory transactions with detail view.
/// </summary>
public class InventoryTransactionListViewModel : ViewModelBase
{
    private readonly IInventoryApiService _inventoryService;
    private readonly IDialogService _dialogService;

    private ObservableCollection<InventoryTransactionDto> _transactions = new();
    private InventoryTransactionDto? _selectedTransaction;
    private ObservableCollection<InventoryTransactionLineDto> _selectedLines = new();
    private int? _warehouseFilter;
    private int? _transactionTypeFilter;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private int _page = 1;
    private int _totalPages = 1;
    private int _totalRecords;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _hasSelection;

    // Available warehouses and transaction types for filter dropdowns
    private ObservableCollection<FilterOption> _warehouseOptions = new();
    private ObservableCollection<FilterOption> _transactionTypeOptions = new();

    public InventoryTransactionListViewModel()
        : this(App.GetService<IInventoryApiService>(), App.GetService<IDialogService>())
    {
    }

    public InventoryTransactionListViewModel(IInventoryApiService inventoryService, IDialogService dialogService)
    {
        _inventoryService = inventoryService;
        _dialogService = dialogService;
        SetDialogService(dialogService);

        TransactionTypeOptions = new ObservableCollection<FilterOption>
        {
            new(0, "الكل"),
            new(1, "مشتريات"),
            new(2, "مرتجع مشتريات"),
            new(3, "مبيعات"),
            new(4, "مرتجع مبيعات"),
            new(5, "تحويل خارج"),
            new(6, "تحويل داخل"),
            new(7, "جرد"),
            new(8, "تسوية"),
            new(9, "تلف"),
            new(10, "رصيد افتتاحي"),
            new(11, "صرف داخلي"),
            new(12, "استلام داخلي")
        };

        InitializeCommands();
        _ = LoadInitialAsync();
    }

    private async Task LoadInitialAsync()
    {
        // Load warehouses for filter — actually, we'll just let the API handle this.
        await ExecuteAsync(LoadTransactionsOperationAsync);
    }

    private void InitializeCommands()
    {
        SearchCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadTransactionsOperationAsync)));
        NextPageCommand = new RelayCommand(NextPage, () => Page < TotalPages);
        PrevPageCommand = new RelayCommand(PrevPage, () => Page > 1);
        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadTransactionsOperationAsync)));
    }

    #region Properties

    public ObservableCollection<InventoryTransactionDto> Transactions
    {
        get => _transactions;
        set => SetProperty(ref _transactions, value);
    }

    public InventoryTransactionDto? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (SetProperty(ref _selectedTransaction, value))
            {
                HasSelection = value != null;
                UpdateSelectedLines();
            }
        }
    }

    public ObservableCollection<InventoryTransactionLineDto> SelectedLines
    {
        get => _selectedLines;
        set => SetProperty(ref _selectedLines, value);
    }

    public ObservableCollection<FilterOption> WarehouseOptions
    {
        get => _warehouseOptions;
        set => SetProperty(ref _warehouseOptions, value);
    }

    public ObservableCollection<FilterOption> TransactionTypeOptions
    {
        get => _transactionTypeOptions;
        set => SetProperty(ref _transactionTypeOptions, value);
    }

    public int? WarehouseFilter
    {
        get => _warehouseFilter;
        set => SetProperty(ref _warehouseFilter, value);
    }

    public int? TransactionTypeFilter
    {
        get => _transactionTypeFilter;
        set => SetProperty(ref _transactionTypeFilter, value);
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public int Page
    {
        get => _page;
        set
        {
            if (SetProperty(ref _page, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalRecords
    {
        get => _totalRecords;
        set => SetProperty(ref _totalRecords, value);
    }

    public string PageDisplay => $"الصفحة {Page} من {TotalPages} — {TotalRecords} حركة";

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public bool HasSelection
    {
        get => _hasSelection;
        set => SetProperty(ref _hasSelection, value);
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand NextPageCommand { get; private set; } = null!;
    public ICommand PrevPageCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public Task LoadTransactionsAsync() => ExecuteAsync(LoadTransactionsOperationAsync);

    private async Task LoadTransactionsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _inventoryService.GetMovementsAsync(
            productId: null,
            warehouseId: WarehouseFilter,
            movementType: TransactionTypeFilter,
            page: Page,
            pageSize: 50);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Transactions.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Transactions.Add(item);
                }
                // Reset total pages calculation - we don't get total count from this API,
                // so let's estimate based on what we get
                TotalPages = result.Value.Count < 50 ? (Page > 0 ? Page : 1) : Page + 1;
                IsEmpty = Transactions.Count == 0;
                if (!HasSelection && Transactions.Count > 0)
                    SelectedTransaction = Transactions[0];
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل حركات المخزون", "InventoryTransactionListViewModel.LoadTransactionsOperationAsync");
            IsEmpty = Transactions.Count == 0;
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
        }
    }

    private void UpdateSelectedLines()
    {
        SelectedLines.Clear();
        if (_selectedTransaction?.Lines != null)
        {
            foreach (var line in _selectedTransaction.Lines)
            {
                SelectedLines.Add(line);
            }
        }
    }

    private void NextPage()
    {
        if (Page < TotalPages)
        {
            Page++;
            _ = LoadTransactionsAsync();
        }
    }

    private void PrevPage()
    {
        if (Page > 1)
        {
            Page--;
            _ = LoadTransactionsAsync();
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}

/// <summary>
/// Simple key-value for filter dropdowns.
/// </summary>
public record FilterOption(int Value, string DisplayName);
