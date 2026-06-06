using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Cash Flow Report — displays income, expense, and net cash flow
/// with optional cash box filter.
/// </summary>
public class CashFlowReportViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private ICashBoxApiService? _cashBoxApiService;

    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();
    private ICashBoxApiService CashBoxApiService => _cashBoxApiService ??= App.GetService<ICashBoxApiService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private DateTime _dateFrom;
    private DateTime _dateTo;
    private int? _selectedCashBoxId;
    private string? _errorMessage;
    private decimal _openingBalance;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private decimal _netCashFlow;
    private decimal _closingBalance;
    private bool _hasData;

    public CashFlowReportViewModel()
    {
        _dateTo = DateTime.Today;
        _dateFrom = DateTime.Today.AddDays(-30);

        CashBoxes = new ObservableCollection<CashBoxDto>();
        IncomeItems = new ObservableCollection<CashFlowItemDto>();
        ExpenseItems = new ObservableCollection<CashFlowItemDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        LoadCashBoxesCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCashBoxesAsync)));

        _ = LoadCashBoxesAsync();
    }

    #region Properties

    public DateTime DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public ObservableCollection<CashBoxDto> CashBoxes { get; }

    /// <summary>
    /// Selected CashBox ID. Null means all cash boxes.
    /// </summary>
    public int? SelectedCashBoxId
    {
        get => _selectedCashBoxId;
        set => SetProperty(ref _selectedCashBoxId, value is 0 ? null : value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Income items for the cash flow report
    /// </summary>
    public ObservableCollection<CashFlowItemDto> IncomeItems { get; }

    /// <summary>
    /// Expense items for the cash flow report
    /// </summary>
    public ObservableCollection<CashFlowItemDto> ExpenseItems { get; }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        private set
        {
            if (SetProperty(ref _openingBalance, value))
                OnPropertyChanged(nameof(FormattedOpeningBalance));
        }
    }

    public decimal TotalIncome
    {
        get => _totalIncome;
        private set
        {
            if (SetProperty(ref _totalIncome, value))
                OnPropertyChanged(nameof(FormattedTotalIncome));
        }
    }

    public decimal TotalExpense
    {
        get => _totalExpense;
        private set
        {
            if (SetProperty(ref _totalExpense, value))
                OnPropertyChanged(nameof(FormattedTotalExpense));
        }
    }

    public decimal NetCashFlow
    {
        get => _netCashFlow;
        private set
        {
            if (SetProperty(ref _netCashFlow, value))
            {
                OnPropertyChanged(nameof(FormattedNetCashFlow));
                OnPropertyChanged(nameof(IsPositiveCashFlow));
            }
        }
    }

    public decimal ClosingBalance
    {
        get => _closingBalance;
        private set
        {
            if (SetProperty(ref _closingBalance, value))
                OnPropertyChanged(nameof(FormattedClosingBalance));
        }
    }

    public string FormattedOpeningBalance => OpeningBalance.ToString("N2");
    public string FormattedTotalIncome => TotalIncome.ToString("N2");
    public string FormattedTotalExpense => TotalExpense.ToString("N2");
    public string FormattedNetCashFlow => NetCashFlow.ToString("N2");
    public string FormattedClosingBalance => ClosingBalance.ToString("N2");

    public bool IsPositiveCashFlow => NetCashFlow >= 0;

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public bool IsEmpty => !HasData;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand LoadCashBoxesCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadCashBoxesAsync()
    {
        try
        {
            var result = await CashBoxApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    CashBoxes.Clear();

                    // Cannot easily create a dummy CashBoxDto for "All" — use null selection instead
                    foreach (var cb in result.Value.Where(cb => cb.IsActive))
                        CashBoxes.Add(cb);

                    SelectedCashBoxId = null;
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة الصناديق", "CashFlowReportViewModel.LoadCashBoxesAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading cash flow report from {DateFrom} to {DateTo}, CashBoxId: {CashBoxId}",
            DateFrom, DateTo, SelectedCashBoxId);

        var result = await ReportApiService.GetCashFlowReportAsync(DateFrom, DateTo, SelectedCashBoxId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                var report = result.Value;

                IncomeItems.Clear();
                ExpenseItems.Clear();

                // Populate income items
                if (report.IncomeItems != null)
                {
                    foreach (var item in report.IncomeItems)
                        IncomeItems.Add(item);
                }

                // Populate expense items
                if (report.ExpenseItems != null)
                {
                    foreach (var item in report.ExpenseItems)
                        ExpenseItems.Add(item);
                }

                // Set summary values
                OpeningBalance = report.OpeningBalance;
                TotalIncome = report.TotalIncome;
                TotalExpense = report.TotalExpense;
                NetCashFlow = report.NetCashFlow;
                ClosingBalance = report.ClosingBalance;

                HasData = true;

                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Cash flow report loaded: Opening={OpeningBalance}, Income={TotalIncome}, Expense={TotalExpense}, Net={NetCashFlow}, Closing={ClosingBalance}",
                OpeningBalance, TotalIncome, TotalExpense, NetCashFlow, ClosingBalance);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير التدفق النقدي", "CashFlowReportViewModel.LoadAsync");
            Log.Warning("Failed to load cash flow report: {Error}", result.Error);
        }
    }

    #endregion
}
