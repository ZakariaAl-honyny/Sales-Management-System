using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Income Statement Report — displays revenue, cost, and net profit/loss
/// for a given date range.
/// </summary>
public class IncomeStatementViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private decimal _totalRevenue;
    private decimal _totalCost;
    private decimal _netProfit;

    public IncomeStatementViewModel()
    {
        _dateTo = DateTime.Today;
        _dateFrom = DateTime.Today.AddDays(-30);

        ReportData = new ObservableCollection<IncomeStatementDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));
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

    public ObservableCollection<IncomeStatementDto> ReportData { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set
        {
            if (SetProperty(ref _totalRevenue, value))
                OnPropertyChanged(nameof(FormattedTotalRevenue));
        }
    }

    public decimal TotalCost
    {
        get => _totalCost;
        private set
        {
            if (SetProperty(ref _totalCost, value))
                OnPropertyChanged(nameof(FormattedTotalCost));
        }
    }

    public decimal NetProfit
    {
        get => _netProfit;
        private set
        {
            if (SetProperty(ref _netProfit, value))
            {
                OnPropertyChanged(nameof(FormattedNetProfit));
                OnPropertyChanged(nameof(IsProfit));
                OnPropertyChanged(nameof(IsLoss));
                OnPropertyChanged(nameof(ProfitLossLabel));
            }
        }
    }

    public string FormattedTotalRevenue => TotalRevenue.ToString("N2");
    public string FormattedTotalCost => TotalCost.ToString("N2");
    public string FormattedNetProfit => Math.Abs(NetProfit).ToString("N2");

    public bool IsProfit => NetProfit >= 0;
    public bool IsLoss => NetProfit < 0;
    public string ProfitLossLabel => NetProfit >= 0 ? "صافي الربح" : "صافي الخسارة";

    /// <summary>
    /// True when there are report rows
    /// </summary>
    public bool HasData => ReportData.Count > 0;

    /// <summary>
    /// True when no data — show empty state
    /// </summary>
    public bool IsEmpty => ReportData.Count == 0;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading income statement report from {DateFrom} to {DateTo}", DateFrom, DateTo);

        var result = await ReportApiService.GetIncomeStatementAsync(DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                ReportData.Clear();

                foreach (var item in result.Value)
                {
                    ReportData.Add(item);
                }

                // Calculate totals from the returned data
                TotalRevenue = result.Value
                    .Where(x => x.Category == "إيرادات المبيعات" || x.Category == "الإيرادات")
                    .Sum(x => x.Amount);

                TotalCost = result.Value
                    .Where(x => x.Category == "تكلفة المشتريات" || x.Category == "المصروفات" || x.Category == "تكلفة المبيعات")
                    .Sum(x => Math.Abs(x.Amount));

                NetProfit = TotalRevenue - TotalCost;

                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Income statement loaded: Revenue={TotalRevenue}, Cost={TotalCost}, Net={NetProfit}",
                TotalRevenue, TotalCost, NetProfit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل قائمة الدخل", "IncomeStatementViewModel.LoadAsync");
            Log.Warning("Failed to load income statement: {Error}", result.Error);
        }
    }

    #endregion

    #region Export

    private async Task ExportPdfAsync()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("التصنيف", typeof(string));
            dataTable.Columns.Add("البيان", typeof(string));
            dataTable.Columns.Add("المبلغ", typeof(decimal));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.Category, item.Description, item.Amount);

            await PdfExportService.ExportToPdfAsync("قائمة الدخل", dataTable, NetProfit,
                $"IncomeStatement_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير قائمة الدخل إلى PDF", "IncomeStatementViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
