using System.Collections.ObjectModel;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

public class ProfitByCustomerViewModel : ViewModelBase
{
    private ISalesReportApiService? _salesReportApiService;
    private ICustomerApiService? _customerApiService;

    private ISalesReportApiService SalesReportApiService => _salesReportApiService ??= App.GetService<ISalesReportApiService>();
    private ICustomerApiService CustomerApiService => _customerApiService ??= App.GetService<ICustomerApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedCustomerId;
    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<ProfitByCustomerDto> _reportData = new();

    public ProfitByCustomerViewModel()
    {
        _fromDate = DateTime.Today.AddDays(-30);
        _toDate = DateTime.Today;

        Customers = new ObservableCollection<CustomerDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = LoadCustomersAsync();
        _ = LoadDataAsync();
    }

    #region Properties

    public ObservableCollection<CustomerDto> Customers { get; }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
                _ = LoadDataAsync();
        }
    }

    public ObservableCollection<ProfitByCustomerDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalSales));
                OnPropertyChanged(nameof(TotalProfit));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    public bool HasData => ReportData.Count > 0;
    public bool IsEmpty => ReportData.Count == 0 && HasSearched;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalSales => ReportData.Sum(x => x.TotalSales);
    public decimal TotalProfit => ReportData.Sum(x => x.GrossProfit);
    public string SummaryText => $"إجمالي المبيعات: {TotalSales:N2} — إجمالي الربح: {TotalProfit:N2}";

    #endregion

    #region Commands

    public AsyncRelayCommand LoadCommand { get; }
    public RelayCommand ExportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading profit by customer report (From: {FromDate}, To: {ToDate})", FromDate, ToDate);

        var result = await SalesReportApiService.GetProfitByCustomerAsync(SelectedCustomerId, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.GrossProfit)
                .ToList();

            ReportData = new ObservableCollection<ProfitByCustomerDto>(sorted);
            HasSearched = true;

            Log.Information("Profit by customer loaded: {Count} customers, TotalSales={TotalSales}, TotalProfit={TotalProfit}",
                ReportData.Count, TotalSales, TotalProfit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير الربح حسب العميل", "ProfitByCustomerViewModel.LoadDataAsync");
            Log.Warning("Failed to load profit by customer: {Error}", result.Error);
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var result = await CustomerApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Customers.Clear();
                    Customers.Add(new CustomerDto(0, "جميع العملاء", null, null, null, null, 0, true, 0));
                    foreach (var c in result.Value)
                        Customers.Add(c);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة العملاء", "ProfitByCustomerViewModel.LoadCustomersAsync", ex);
        }
    }

    #endregion

    #region Export

    private async void ExportToExcel()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ProfitByCustomer_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("الربح حسب العميل");

                    worksheet.Cell(1, 1).Value = "العميل";
                    worksheet.Cell(1, 2).Value = "إجمالي المبيعات";
                    worksheet.Cell(1, 3).Value = "إجمالي التكلفة";
                    worksheet.Cell(1, 4).Value = "صافي الربح";
                    worksheet.Cell(1, 5).Value = "نسبة الربح";
                    worksheet.Cell(1, 6).Value = "عدد الفواتير";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.CustomerName;
                        worksheet.Cell(i + 2, 2).Value = item.TotalSales;
                        worksheet.Cell(i + 2, 3).Value = item.TotalCost;
                        worksheet.Cell(i + 2, 4).Value = item.GrossProfit;
                        worksheet.Cell(i + 2, 5).Value = item.ProfitMargin.ToString("P1");
                        worksheet.Cell(i + 2, 6).Value = item.InvoiceCount;
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير الربح حسب العميل إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير الربح حسب العميل إلى Excel", "ProfitByCustomerViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

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
            dataTable.Columns.Add("العميل", typeof(string));
            dataTable.Columns.Add("إجمالي المبيعات", typeof(decimal));
            dataTable.Columns.Add("التكلفة", typeof(decimal));
            dataTable.Columns.Add("الربح", typeof(decimal));
            dataTable.Columns.Add("نسبة الربح", typeof(string));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.CustomerName, item.TotalSales,
                    item.TotalCost, item.GrossProfit, item.ProfitMargin);

            await PdfExportService.ExportToPdfAsync("الربح حسب العميل", dataTable, ReportData.Sum(x => x.GrossProfit),
                $"ProfitByCustomer_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير الربح حسب العميل إلى PDF", "ProfitByCustomerViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
