using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Daily Closure Report — displays daily cash box closures.
/// </summary>
public class DailyClosureReportViewModel : ViewModelBase
{
    private ICashBoxReportApiService? _cashBoxReportApiService;
    private ICashBoxApiService? _cashBoxApiService;

    private ICashBoxReportApiService CashBoxReportApiService => _cashBoxReportApiService ??= App.GetService<ICashBoxReportApiService>();
    private ICashBoxApiService CashBoxApiService => _cashBoxApiService ??= App.GetService<ICashBoxApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private int? _selectedCashBoxId;
    private decimal _totalOpening;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private decimal _totalClosing;
    private bool _hasData;

    public DailyClosureReportViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<DailyClosureReportDto>();
        CashBoxes = new ObservableCollection<CashBoxDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        LoadCashBoxesCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCashBoxesAsync)));

        _ = LoadCashBoxesAsync();
    }

    #region Properties

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

    public int? SelectedCashBoxId
    {
        get => _selectedCashBoxId;
        set => SetProperty(ref _selectedCashBoxId, value);
    }

    public ObservableCollection<DailyClosureReportDto> Entries { get; }
    public ObservableCollection<CashBoxDto> CashBoxes { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalOpening
    {
        get => _totalOpening;
        private set
        {
            if (SetProperty(ref _totalOpening, value))
                OnPropertyChanged(nameof(FormattedTotalOpening));
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

    public decimal TotalClosing
    {
        get => _totalClosing;
        private set
        {
            if (SetProperty(ref _totalClosing, value))
                OnPropertyChanged(nameof(FormattedTotalClosing));
        }
    }

    public string FormattedTotalOpening => TotalOpening.ToString("N2");
    public string FormattedTotalIncome => TotalIncome.ToString("N2");
    public string FormattedTotalExpense => TotalExpense.ToString("N2");
    public string FormattedTotalClosing => TotalClosing.ToString("N2");

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
                    foreach (var box in result.Value.Where(b => b.IsActive))
                        CashBoxes.Add(box);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة الصناديق", "DailyClosureReportViewModel.LoadCashBoxesAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading daily closure report from {FromDate} to {ToDate}, CashBox={CashBoxId}",
            FromDate, ToDate, SelectedCashBoxId);

        var result = await CashBoxReportApiService.GetDailyClosureReportAsync(FromDate, ToDate, SelectedCashBoxId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.ClosureDate))
                {
                    Entries.Add(item);
                }

                TotalOpening = result.Value.Sum(x => x.OpeningBalance);
                TotalIncome = result.Value.Sum(x => x.TotalIncome);
                TotalExpense = result.Value.Sum(x => x.TotalExpense);
                TotalClosing = result.Value.Sum(x => x.ExpectedClosingBalance);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Daily closure report loaded: {Count} records", Entries.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير الإغلاق اليومي", "DailyClosureReportViewModel.LoadAsync");
            Log.Warning("Failed to load daily closure report: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
