using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using SalesSystem.Contracts.Responses;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Daily Closure Report — uses CashBoxSummaryDto from the API and maps
/// to DailyClosureReportDto for display. DailyClosure entity is deferred to V2,
/// so we use the cash box summary endpoint which provides the same balance data.
/// </summary>
public class DailyClosureReportViewModel : ViewModelBase
{
    private ICashBoxReportApiService? _cashBoxReportApiService;
    private ICashBoxApiService? _cashBoxApiService;

    private ICashBoxReportApiService CashBoxReportApiService => _cashBoxReportApiService ??= App.GetService<ICashBoxReportApiService>();
    private ICashBoxApiService CashBoxApiService => _cashBoxApiService ??= App.GetService<ICashBoxApiService>();

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _fromDate;
    private DateTime _toDate;
    private int? _selectedCashBoxId;
    private string? _errorMessage;
    private bool _hasData;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private decimal _totalNetBalance;

    public DailyClosureReportViewModel()
    {
        _fromDate = DateTime.Today;
        _toDate = DateTime.Today;

        CashBoxes = new ObservableCollection<CashBoxDto>();
        Entries = new ObservableCollection<DailyClosureReportDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadReportAsync, "جاري تحميل تقرير الإغلاق اليومي...")));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = ExecuteAsync(LoadCashBoxesCoreAsync);
    }

    #region Properties

    public ObservableCollection<CashBoxDto> CashBoxes { get; }

    public ObservableCollection<DailyClosureReportDto> Entries { get; }

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

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasData
    {
        get => _hasData;
        private set
        {
            if (SetProperty(ref _hasData, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public bool IsEmpty => !HasData;

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

    public decimal TotalNetBalance
    {
        get => _totalNetBalance;
        private set
        {
            if (SetProperty(ref _totalNetBalance, value))
                OnPropertyChanged(nameof(FormattedTotalNetBalance));
        }
    }

    public string FormattedTotalIncome => TotalIncome.ToString("N2");
    public string FormattedTotalExpense => TotalExpense.ToString("N2");
    public string FormattedTotalNetBalance => TotalNetBalance.ToString("N2");

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadCashBoxesCoreAsync()
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
        else
        {
            HandleFailure(result.Error ?? "فشل في تحميل قائمة الصناديق", "DailyClosureReportViewModel.LoadCashBoxesCoreAsync");
        }
    }

    private async Task LoadReportAsync()
    {
        ErrorMessage = null;

        // Use ToDate as the as-of date for the summary — the report reflects
        // cash box balances up to and including the selected end date.
        var asOfDate = ToDate;

        Log.Information("Loading daily closure report as of {AsOfDate}, cashBoxId={CashBoxId}",
            asOfDate, SelectedCashBoxId);

        var result = await CashBoxReportApiService.GetCashBoxSummaryAsync(asOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            var filtered = result.Value.AsEnumerable();

            // Filter by cash box if a specific one is selected
            if (SelectedCashBoxId.HasValue)
                filtered = filtered.Where(x => x.CashBoxId == SelectedCashBoxId.Value);

            var entries = filtered
                .OrderBy(x => x.CashBoxName)
                .Select(x => new DailyClosureReportDto(
                    Date: asOfDate,
                    CashBoxName: x.CashBoxName,
                    TotalIncome: x.TotalIncome,
                    TotalExpense: x.TotalExpense,
                    NetBalance: x.NetBalance,
                    IsReconciled: true)) // DailyClosure entity deferred to V2 — default to reconciled
                .ToList();

            InvokeOnUIThread(() =>
            {
                Entries.Clear();
                foreach (var entry in entries)
                    Entries.Add(entry);

                TotalIncome = entries.Sum(x => x.TotalIncome);
                TotalExpense = entries.Sum(x => x.TotalExpense);
                TotalNetBalance = entries.Sum(x => x.NetBalance);

                HasData = entries.Count > 0;
            });

            Log.Information("Daily closure report loaded: {Count} entries", entries.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير الإغلاق اليومي",
                "DailyClosureReportViewModel.LoadReportAsync");

            InvokeOnUIThread(() =>
            {
                Entries.Clear();
                HasData = false;
                TotalIncome = 0;
                TotalExpense = 0;
                TotalNetBalance = 0;
            });
        }
    }

    #endregion

    #region Export

    private async Task ExportPdfAsync()
    {
        var dialog = DialogService;
        if (dialog == null) return;

        if (!HasData)
        {
            await dialog.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("التاريخ", typeof(string));
            dataTable.Columns.Add("الصندوق", typeof(string));
            dataTable.Columns.Add("الإيرادات", typeof(decimal));
            dataTable.Columns.Add("المصروفات", typeof(decimal));
            dataTable.Columns.Add("صافي الرصيد", typeof(decimal));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.Date.ToString("yyyy/MM/dd"),
                    item.CashBoxName,
                    item.TotalIncome,
                    item.TotalExpense,
                    item.NetBalance);

            await PdfExportService.ExportToPdfAsync("تقرير الإغلاق اليومي", dataTable, TotalNetBalance,
                $"DailyClosure_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير الإغلاق اليومي إلى PDF", "DailyClosureReportViewModel.ExportPdf", ex);
            if (dialog != null)
                await dialog.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
