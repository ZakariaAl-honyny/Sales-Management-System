using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Cash Box Summary Report — displays balances of all cash boxes.
/// </summary>
public class CashBoxSummaryViewModel : ViewModelBase
{
    private ICashBoxReportApiService? _cashBoxReportApiService;
    private ICashBoxReportApiService CashBoxReportApiService => _cashBoxReportApiService ??= App.GetService<ICashBoxReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private bool _hasData;

    public CashBoxSummaryViewModel()
    {
        _asOfDate = DateTime.Today;

        Entries = new ObservableCollection<CashBoxSummaryDto>();

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

    public ObservableCollection<CashBoxSummaryDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
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

    public string FormattedTotalIncome => TotalIncome.ToString("N2");
    public string FormattedTotalExpense => TotalExpense.ToString("N2");

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

        Log.Information("Loading cash box summary as of {AsOfDate}", AsOfDate);

        var result = await CashBoxReportApiService.GetCashBoxSummaryAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderBy(x => x.CashBoxName))
                {
                    Entries.Add(item);
                }

                TotalIncome = result.Value.Sum(x => x.TotalIncome);
                TotalExpense = result.Value.Sum(x => x.TotalExpense);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Cash box summary loaded: {Count} boxes, TotalIncome={TotalIncome}, TotalExpense={TotalExpense}",
                Entries.Count, TotalIncome, TotalExpense);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ملخص الصناديق", "CashBoxSummaryViewModel.LoadAsync");
            Log.Warning("Failed to load cash box summary: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
