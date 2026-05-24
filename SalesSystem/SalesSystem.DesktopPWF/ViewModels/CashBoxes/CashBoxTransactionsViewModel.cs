using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

public class CashBoxTransactionsViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<CashTransactionDto> _transactions = new();
    private CashTransactionDto? _selectedTransaction;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _cashBoxId;
    private string _cashBoxName = string.Empty;
    private decimal _currentBalance;
    private DateTime? _filterFrom;
    private DateTime? _filterTo;
    private decimal _expenseAmount;
    private string? _expenseNotes;

    public CashBoxTransactionsViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashBoxTransactionsViewModel(
        ICashBoxApiService cashBoxService,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        SetDialogService(dialogService);
        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadTransactionsAsync);
        RecordExpenseCommand = new AsyncRelayCommand(RecordExpenseAsync);
        FilterCommand = new AsyncRelayCommand(LoadTransactionsAsync);
        ClearFilterCommand = new RelayCommand(ClearFilter);
    }

    #region Properties

    public int CashBoxId
    {
        get => _cashBoxId;
        set => SetProperty(ref _cashBoxId, value);
    }

    public string CashBoxName
    {
        get => _cashBoxName;
        set => SetProperty(ref _cashBoxName, value);
    }

    public ObservableCollection<CashTransactionDto> Transactions
    {
        get => _transactions;
        set => SetProperty(ref _transactions, value);
    }

    public CashTransactionDto? SelectedTransaction
    {
        get => _selectedTransaction;
        set => SetProperty(ref _selectedTransaction, value);
    }

    public decimal CurrentBalance
    {
        get => _currentBalance;
        set => SetProperty(ref _currentBalance, value);
    }

    public DateTime? FilterFrom
    {
        get => _filterFrom;
        set => SetProperty(ref _filterFrom, value);
    }

    public DateTime? FilterTo
    {
        get => _filterTo;
        set => SetProperty(ref _filterTo, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public decimal ExpenseAmount
    {
        get => _expenseAmount;
        set => SetProperty(ref _expenseAmount, value);
    }

    public string? ExpenseNotes
    {
        get => _expenseNotes;
        set => SetProperty(ref _expenseNotes, value);
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand RecordExpenseCommand { get; private set; } = null!;
    public ICommand FilterCommand { get; private set; } = null!;
    public ICommand ClearFilterCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadTransactionsAsync()
    {
        await ExecuteAsync(LoadTransactionsOperationAsync);
    }

    private async Task LoadTransactionsOperationAsync()
    {
        ErrorMessage = null;

        DateOnly? from = FilterFrom.HasValue ? DateOnly.FromDateTime(FilterFrom.Value) : null;
        DateOnly? to = FilterTo.HasValue ? DateOnly.FromDateTime(FilterTo.Value) : null;
        var result = await _cashBoxService.GetTransactionsAsync(CashBoxId, from, to);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Transactions.Clear();
                var sorted = result.Value.OrderByDescending(x => x.CreatedAt);
                foreach (var item in sorted)
                {
                    Transactions.Add(item);
                }
                IsEmpty = Transactions.Count == 0;
                CurrentBalance = Transactions.Count > 0 ? Transactions[0].BalanceAfter : 0m;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل حركات الصندوق", "CashBoxTransactionsViewModel.LoadTransactionsOperationAsync", "[CashBoxTransactionsViewModel.LoadTransactionsOperationAsync] Failed to load cash box transactions from API.");
            IsEmpty = Transactions.Count == 0;
        }
    }

    private async Task RecordExpenseAsync()
    {
        if (ExpenseAmount <= 0)
        {
            await _dialogService.ShowWarningAsync("تسجيل مصروف", "يرجى إدخال مبلغ المصروف");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تسجيل مصروف", $"هل تريد تسجيل مصروف بمبلغ {ExpenseAmount:N2}؟");
        if (!confirmed) return;

        await ExecuteAsync(RecordExpenseOperationAsync);
    }

    private async Task RecordExpenseOperationAsync()
    {
        ErrorMessage = null;
        var request = new AddCashTransactionRequest(ExpenseAmount, ExpenseNotes);
        var result = await _cashBoxService.RecordExpenseAsync(CashBoxId, request);

        if (result.IsSuccess)
        {
            ExpenseAmount = 0;
            ExpenseNotes = null;
            _toastService.ShowSuccess("تم تسجيل المصروف بنجاح");
            await LoadTransactionsAsync();
        }
        else
        {
            var error = result.Error ?? "فشل في تسجيل المصروف";
            ErrorMessage = HandleFailure(error, "CashBoxTransactionsViewModel.RecordExpenseOperationAsync", "[CashBoxTransactionsViewModel.RecordExpenseOperationAsync] Failed to record expense.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    private void ClearFilter()
    {
        FilterFrom = null;
        FilterTo = null;
        _ = LoadTransactionsAsync();
    }

    #endregion
}
