using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

/// <summary>
/// ViewModel for Daily Closure management — shows summary cards, past closures,
/// and supports reconciliation with actual cash count (Phase 29).
/// </summary>
public class DailyClosureViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _cashBoxId;
    private string _cashBoxName = string.Empty;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private decimal _openingBalance;
    private decimal _projectedClosingBalance;
    private decimal _actualCashCount;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _isClosedToday;
    private DailyClosureDto? _selectedClosure;

    public DailyClosureViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public DailyClosureViewModel(
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
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        PerformClosureCommand = new AsyncRelayCommand(PerformClosureAsync);
        ReconcileCommand = new AsyncRelayCommand(ReconcileAsync);
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

    public decimal TotalIncome
    {
        get => _totalIncome;
        private set => SetProperty(ref _totalIncome, value);
    }

    public decimal TotalExpense
    {
        get => _totalExpense;
        private set => SetProperty(ref _totalExpense, value);
    }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        private set => SetProperty(ref _openingBalance, value);
    }

    public decimal ProjectedClosingBalance
    {
        get => _projectedClosingBalance;
        private set => SetProperty(ref _projectedClosingBalance, value);
    }

    /// <summary>
    /// Actual cash counted by the user during daily closure reconciliation.
    /// </summary>
    public decimal ActualCashCount
    {
        get => _actualCashCount;
        set
        {
            if (SetProperty(ref _actualCashCount, value))
            {
                OnPropertyChanged(nameof(Difference));
                OnPropertyChanged(nameof(DifferenceDisplay));
                OnPropertyChanged(nameof(IsBalanced));
            }
        }
    }

    /// <summary>
    /// Difference between actual cash count and projected closing balance.
    /// </summary>
    public decimal Difference => ActualCashCount - ProjectedClosingBalance;

    /// <summary>
    /// Formatted difference string with color indicator (green when balanced, red when unbalanced).
    /// </summary>
    public string DifferenceDisplay
    {
        get
        {
            if (Math.Abs(Difference) < 0.01m)
                return "مطابق ✅";
            return Difference > 0
                ? $"زيادة قدرها {Difference:N2} ⚠️"
                : $"عجز قدره {Math.Abs(Difference):N2} ⚠️";
        }
    }

    /// <summary>
    /// True when the actual cash count matches (within rounding) the projected closing balance.
    /// </summary>
    public bool IsBalanced => Math.Abs(Difference) < 0.01m;

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

    public bool IsClosedToday
    {
        get => _isClosedToday;
        private set => SetProperty(ref _isClosedToday, value);
    }

    /// <summary>
    /// Returns a brief status string for the closure state.
    /// </summary>
    public string ReconciliationStatus
    {
        get
        {
            if (IsClosedToday && IsBalanced) return "مقفل ومطابق";
            if (IsClosedToday && !IsBalanced) return "مقفل مع فروقات";
            return "غير مقفل";
        }
    }

    public DailyClosureDto? SelectedClosure
    {
        get => _selectedClosure;
        set => SetProperty(ref _selectedClosure, value);
    }

    public ObservableCollection<DailyClosureDto> PastClosures { get; } = new();

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand PerformClosureCommand { get; private set; } = null!;
    public ICommand ReconcileCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteAsync(LoadDataOperationAsync);
    }

    private async Task LoadDataOperationAsync()
    {
        ErrorMessage = null;
        var today = DateOnly.FromDateTime(DateTime.Now);

        var closuresResult = await _cashBoxService.GetDailyClosuresAsync(CashBoxId);
        if (!closuresResult.IsSuccess || closuresResult.Value == null)
        {
            ErrorMessage = HandleFailure(closuresResult.Error ?? "فشل في تحميل الإغلاقات اليومية",
                "DailyClosureViewModel.LoadDataOperationAsync",
                "[DailyClosureViewModel.LoadDataOperationAsync] Failed to load daily closures.");
            return;
        }

        var transactionsResult = await _cashBoxService.GetTransactionsAsync(CashBoxId, today, today);
        if (!transactionsResult.IsSuccess || transactionsResult.Value == null)
        {
            ErrorMessage = HandleFailure(transactionsResult.Error ?? "فشل في تحميل حركات اليوم",
                "DailyClosureViewModel.LoadDataOperationAsync",
                "[DailyClosureViewModel.LoadDataOperationAsync] Failed to load today's transactions.");
            return;
        }

        InvokeOnUIThread(() =>
        {
            PastClosures.Clear();
            var sorted = closuresResult.Value.OrderByDescending(c => c.ClosureDate);
            foreach (var closure in sorted)
            {
                PastClosures.Add(closure);
            }

            IsEmpty = PastClosures.Count == 0;
            IsClosedToday = closuresResult.Value.Any(c => c.ClosureDate == today);

            var lastClosure = closuresResult.Value
                .Where(c => c.ClosureDate < today)
                .OrderByDescending(c => c.ClosureDate)
                .FirstOrDefault();

            OpeningBalance = lastClosure?.ExpectedClosingBalance ?? 0m;

            var incomeTypes = new HashSet<byte> { 1, 2, 5, 8 };
            var expenseTypes = new HashSet<byte> { 3, 4, 6, 7 };

            var todayTxs = transactionsResult.Value;
            TotalIncome = todayTxs.Where(t => incomeTypes.Contains(t.TransactionType)).Sum(t => t.Amount);
            TotalExpense = todayTxs.Where(t => expenseTypes.Contains(t.TransactionType)).Sum(t => t.Amount);
            ProjectedClosingBalance = OpeningBalance + TotalIncome - TotalExpense;

            // Reset reconciliation fields
            ActualCashCount = ProjectedClosingBalance;
            OnPropertyChanged(nameof(ReconciliationStatus));
        });
    }

    public async Task PerformClosureAsync()
    {
        if (IsClosedToday)
        {
            await _dialogService.ShowWarningAsync("إغلاق اليوم", "تم إغلاق هذا الصندوق اليوم بالفعل");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "إغلاق اليوم",
            $"هل أنت متأكد من إغلاق صندوق \"{CashBoxName}\" لهذا اليوم؟\n\n" +
            $"الرصيد الافتتاحي: {OpeningBalance:N2}\n" +
            $"إجمالي الإيرادات: {TotalIncome:N2}\n" +
            $"إجمالي المصروفات: {TotalExpense:N2}\n" +
            $"الرصيد المتوقع بعد الإغلاق: {ProjectedClosingBalance:N2}\n" +
            $"العد الفعلي: {ActualCashCount:N2}\n" +
            $"الفارق: {Difference:N2}");

        if (!confirmed) return;

        await ExecuteAsync(PerformClosureOperationAsync);
    }

    private async Task PerformClosureOperationAsync()
    {
        ErrorMessage = null;

        var result = await _cashBoxService.PerformDailyClosureAsync(CashBoxId);
        if (result.IsSuccess)
        {
            _toastService.ShowSuccess("تم إغلاق اليوم بنجاح");
            await LoadDataAsync();
        }
        else
        {
            var error = result.Error ?? "فشل في إغلاق اليوم";
            ErrorMessage = HandleFailure(error,
                "DailyClosureViewModel.PerformClosureOperationAsync",
                $"[DailyClosureViewModel.PerformClosureOperationAsync] Failed to perform daily closure.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    /// <summary>
    /// Reconciles the daily closure by comparing actual cash count with projected balance.
    /// Shows a toast with the result.
    /// </summary>
    public async Task ReconcileAsync()
    {
        if (!IsClosedToday)
        {
            await _dialogService.ShowWarningAsync("تسوية الإغلاق",
                "يرجى إغلاق اليوم أولاً قبل إجراء التسوية");
            return;
        }

        if (IsBalanced)
        {
            _toastService.ShowSuccess($"التسوية مطابقة ✅ — الرصيد الختامي: {ActualCashCount:N2}");
        }
        else
        {
            _toastService.ShowInfo(
                Difference > 0
                    ? $"الرصيد الفعلي يزيد بمقدار {Difference:N2} عن الرصيد المتوقع ⚠️"
                    : $"الرصيد الفعلي أقل بمقدار {Math.Abs(Difference):N2} عن الرصيد المتوقع ⚠️");
        }
    }

    #endregion
}
