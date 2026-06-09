using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Trial Balance Report — displays debits and credits.
/// </summary>
public class TrialBalanceViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalDebit;
    private decimal _totalCredit;
    private bool _isBalanced;
    private bool _hasData;

    public TrialBalanceViewModel()
    {
        _asOfDate = DateTime.Today;

        Entries = new ObservableCollection<TrialBalanceDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));
    }

    #region Properties

    public DateTime AsOfDate
    {
        get => _asOfDate;
        set => SetProperty(ref _asOfDate, value);
    }

    public ObservableCollection<TrialBalanceDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
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

    public bool IsBalanced
    {
        get => _isBalanced;
        private set
        {
            if (SetProperty(ref _isBalanced, value))
                OnPropertyChanged(nameof(BalanceStatusDisplay));
        }
    }

    public string FormattedTotalDebit => TotalDebit.ToString("N2");
    public string FormattedTotalCredit => TotalCredit.ToString("N2");

    public string BalanceStatusDisplay => IsBalanced ? "🟢 متوازن" : "🔴 غير متوازن";

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

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading trial balance as of {AsOfDate}", AsOfDate);

        var result = await ReportApiService.GetTrialBalanceAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var entry in result.Value.OrderBy(x => x.AccountCode))
                {
                    Entries.Add(entry);
                }

                TotalDebit = result.Value.Sum(x => x.ClosingDebit);
                TotalCredit = result.Value.Sum(x => x.ClosingCredit);
                IsBalanced = Math.Abs(TotalDebit - TotalCredit) < 0.01m;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Trial balance loaded: TotalDebit={TotalDebit}, TotalCredit={TotalCredit}",
                TotalDebit, TotalCredit, IsBalanced);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ميزان المراجعة", "TrialBalanceViewModel.LoadAsync");
            Log.Warning("Failed to load trial balance: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
