using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the General Ledger Report — displays account movements.
/// </summary>
public class GeneralLedgerViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IAccountApiService? _accountApiService;

    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();
    private IAccountApiService AccountApiService => _accountApiService ??= App.GetService<IAccountApiService>();

    private IDialogService D => DialogService!;

    private int? _selectedAccountId;
    private string? _selectedAccountName;
    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _openingBalance;
    private decimal _closingBalance;
    private decimal _totalDebit;
    private decimal _totalCredit;
    private bool _hasData;

    public GeneralLedgerViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Lines = new ObservableCollection<AccountLedgerLineDto>();
        AvailableAccounts = new ObservableCollection<AccountDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        LoadAccountsCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAccountsAsync)));

        _ = LoadAccountsAsync();
    }

    #region Properties

    public int? SelectedAccountId
    {
        get => _selectedAccountId;
        set => SetProperty(ref _selectedAccountId, value);
    }

    public string? SelectedAccountName
    {
        get => _selectedAccountName;
        set => SetProperty(ref _selectedAccountName, value);
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ObservableCollection<AccountLedgerLineDto> Lines { get; }
    public ObservableCollection<AccountDto> AvailableAccounts { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        private set
        {
            if (SetProperty(ref _openingBalance, value))
                OnPropertyChanged(nameof(FormattedOpeningBalance));
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

    public decimal TotalDebit
    {
        get => _totalDebit;
        private set
        {
            if (SetProperty(ref _totalDebit, value))
                OnPropertyChanged(nameof(FormattedTotalDebit));
        }
    }

    public decimal TotalCredit
    {
        get => _totalCredit;
        private set
        {
            if (SetProperty(ref _totalCredit, value))
                OnPropertyChanged(nameof(FormattedTotalCredit));
        }
    }

    public string FormattedOpeningBalance => OpeningBalance.ToString("N2");
    public string FormattedClosingBalance => ClosingBalance.ToString("N2");
    public string FormattedTotalDebit => TotalDebit.ToString("N2");
    public string FormattedTotalCredit => TotalCredit.ToString("N2");

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public bool IsEmpty => !HasData;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }
    public AsyncRelayCommand LoadAccountsCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAccountsAsync()
    {
        try
        {
            var result = await AccountApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    AvailableAccounts.Clear();
                    foreach (var acc in result.Value.Where(a => a.IsActive))
                        AvailableAccounts.Add(acc);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة الحسابات", "GeneralLedgerViewModel.LoadAccountsAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        if (SelectedAccountId == null || SelectedAccountId == 0)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار حساب لعرض دفتر الأستاذ");
            return;
        }

        Log.Information("Loading general ledger for account {AccountId} from {FromDate} to {ToDate}",
            SelectedAccountId, FromDate, ToDate);

        var result = await ReportApiService.GetGeneralLedgerAsync(SelectedAccountId.Value, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Lines.Clear();

                SelectedAccountName = $"{result.Value.AccountCode} - {result.Value.AccountNameAr}";

                if (result.Value.Lines != null)
                {
                    foreach (var line in result.Value.Lines.OrderBy(x => x.Date))
                    {
                        Lines.Add(line);
                    }
                }

                OpeningBalance = result.Value.OpeningBalance;
                TotalDebit = result.Value.TotalDebit;
                TotalCredit = result.Value.TotalCredit;
                ClosingBalance = result.Value.ClosingBalance;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("General ledger loaded: OpeningBalance={OpeningBalance}, TotalDebit={TotalDebit}, TotalCredit={TotalCredit}, ClosingBalance={ClosingBalance}",
                OpeningBalance, TotalDebit, TotalCredit, ClosingBalance);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل دفتر الأستاذ", "GeneralLedgerViewModel.LoadAsync");
            Log.Warning("Failed to load general ledger: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
