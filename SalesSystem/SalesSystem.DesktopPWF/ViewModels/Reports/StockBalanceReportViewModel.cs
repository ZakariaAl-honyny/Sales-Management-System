using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Stock Balance Report — displays stock quantities per warehouse
/// with inventory valuation (Quantity × AverageCost) and low stock indicators.
/// </summary>
public class StockBalanceReportViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IWarehouseApiService? _warehouseApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseApiService => _warehouseApiService ??= App.GetService<IWarehouseApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedWarehouseId;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<StockBalanceReportDto> _reportData = new();

    public StockBalanceReportViewModel()
    {
        Warehouses = new ObservableCollection<WarehouseDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        // Load data on initialization
        _ = LoadWarehousesAsync();
        _ = LoadDataAsync();
    }

    #region Properties

    /// <summary>
    /// Available warehouses for filtering — prepended with "All Warehouses" option.
    /// </summary>
    public ObservableCollection<WarehouseDto> Warehouses { get; }

    /// <summary>
    /// Selected warehouse ID for filtering — null means all warehouses.
    /// </summary>
    public int? SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetProperty(ref _selectedWarehouseId, value))
            {
                _ = LoadDataAsync(); // Reload when filter changes
            }
        }
    }

    /// <summary>
    /// The report data — stock balance items.
    /// </summary>
    public ObservableCollection<StockBalanceReportDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalValue));
                OnPropertyChanged(nameof(LowStockCount));
                OnPropertyChanged(nameof(FormattedTotalValue));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    /// <summary>
    /// True when the report has been loaded at least once.
    /// </summary>
    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    /// <summary>
    /// True when there is data in the report.
    /// </summary>
    public bool HasData => ReportData.Count > 0;

    /// <summary>
    /// True when the report is empty after a search.
    /// </summary>
    public bool IsEmpty => ReportData.Count == 0 && HasSearched;

    /// <summary>
    /// Error message from the last API call.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Total value of all stock items (Quantity × AverageCost).
    /// </summary>
    public decimal TotalValue => ReportData.Sum(x => x.TotalValue);

    /// <summary>
    /// Count of items where stock is below reorder level.
    /// </summary>
    public int LowStockCount => ReportData.Count(x => x.IsLowStock);

    /// <summary>
    /// Formatted total value string (e.g., "1,250,000.00").
    /// </summary>
    public string FormattedTotalValue => TotalValue.ToString("N2");

    /// <summary>
    /// Summary text showing total value and low stock count.
    /// </summary>
    public string SummaryText =>
        $"إجمالي القيمة: {FormattedTotalValue} — عدد الأصناف منخفضة المخزون: {LowStockCount}";

    #endregion

    #region Commands

    /// <summary>
    /// Loads the stock balance report data.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Exports the report data to Excel.
    /// </summary>
    public RelayCommand ExportCommand { get; }

    /// <summary>
    /// Exports the report data to PDF.
    /// </summary>
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading stock balance report (WarehouseId: {WarehouseId})", SelectedWarehouseId);

        var result = await ReportApiService.GetStockBalanceReportAsync(SelectedWarehouseId);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderBy(x => x.ProductName ?? string.Empty)
                .ToList();

            ReportData = new ObservableCollection<StockBalanceReportDto>(sorted);
            HasSearched = true;

            Log.Information("Stock balance report loaded: {Count} items, TotalValue: {TotalValue}, LowStock: {LowStockCount}",
                ReportData.Count, TotalValue, LowStockCount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل كشف رصيد المخازن", "StockBalanceReportViewModel.LoadDataAsync");
            Log.Warning("Failed to load stock balance report: {Error}", result.Error);
        }
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await WarehouseApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Warehouses.Clear();

                    // Add "All Warehouses" option with Id=0
                    Warehouses.Add(new WarehouseDto(0, string.Empty, "جميع المخازن", (byte)1, null, null, null, null, true));

                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المستودعات", "StockBalanceReportViewModel.LoadWarehousesAsync", ex);
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
                FileName = $"StockBalanceReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("كشف رصيد المخازن");

                    // Headers
                    worksheet.Cell(1, 1).Value = "المنتج";
                    worksheet.Cell(1, 2).Value = "التصنيف";
                    worksheet.Cell(1, 3).Value = "المستودع";
                    worksheet.Cell(1, 4).Value = "الكمية";
                    worksheet.Cell(1, 5).Value = "حد الطلب";
                    worksheet.Cell(1, 6).Value = "متوسط التكلفة";
                    worksheet.Cell(1, 7).Value = "القيمة الإجمالية";
                    worksheet.Cell(1, 8).Value = "حالة المخزون";

                    // Data
                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.ProductName;
                        worksheet.Cell(i + 2, 2).Value = item.CategoryName;
                        worksheet.Cell(i + 2, 3).Value = item.WarehouseName;
                        worksheet.Cell(i + 2, 4).Value = item.CurrentStock;
                        worksheet.Cell(i + 2, 5).Value = item.ReorderLevel;
                        worksheet.Cell(i + 2, 6).Value = item.Cost;
                        worksheet.Cell(i + 2, 7).Value = item.TotalValue;
                        worksheet.Cell(i + 2, 8).Value = item.BalanceStatus;
                    }

                    // Styling
                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير التقرير إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف رصيد المخازن إلى Excel", "StockBalanceReportViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("المنتج", typeof(string));
            dataTable.Columns.Add("التصنيف", typeof(string));
            dataTable.Columns.Add("المستودع", typeof(string));
            dataTable.Columns.Add("الكمية", typeof(decimal));
            dataTable.Columns.Add("حد الطلب", typeof(decimal));
            dataTable.Columns.Add("متوسط التكلفة", typeof(decimal));
            dataTable.Columns.Add("القيمة الإجمالية", typeof(decimal));
            dataTable.Columns.Add("حالة المخزون", typeof(string));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.ProductName, item.CategoryName,
                    item.WarehouseName, item.CurrentStock, item.ReorderLevel,
                    item.Cost, item.TotalValue, item.BalanceStatus);

            await PdfExportService.ExportToPdfAsync("كشف رصيد المخازن", dataTable, ReportData.Sum(x => x.TotalValue),
                $"StockBalanceReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف رصيد المخازن إلى PDF", "StockBalanceReportViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
