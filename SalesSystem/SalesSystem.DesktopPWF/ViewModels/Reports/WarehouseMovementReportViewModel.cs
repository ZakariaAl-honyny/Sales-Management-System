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
/// ViewModel for the Warehouse Movement Report — displays inventory movement history
/// with filtering by warehouse and date range.
/// </summary>
public class WarehouseMovementReportViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IWarehouseApiService? _warehouseApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseApiService => _warehouseApiService ??= App.GetService<IWarehouseApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedWarehouseId;
    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<WarehouseMovementReportDto> _reportData = new();

    public WarehouseMovementReportViewModel()
    {
        _dateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); // First day of current month
        _dateTo = DateTime.Today;

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
    /// Start date for the movement report range.
    /// </summary>
    public DateTime DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
                _ = LoadDataAsync();
        }
    }

    /// <summary>
    /// End date for the movement report range.
    /// </summary>
    public DateTime DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
                _ = LoadDataAsync();
        }
    }

    /// <summary>
    /// The report data — warehouse movement items.
    /// </summary>
    public ObservableCollection<WarehouseMovementReportDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalMovements));
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
    /// Total number of movements in the report.
    /// </summary>
    public int TotalMovements => ReportData.Count;

    /// <summary>
    /// Summary text showing total movement count.
    /// </summary>
    public string SummaryText => $"إجمالي الحركات: {TotalMovements}";

    #endregion

    #region Commands

    /// <summary>
    /// Loads the warehouse movement report data.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Exports the report data to Excel.
    /// </summary>
    public RelayCommand ExportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading warehouse movement report (WarehouseId: {WarehouseId}, From: {DateFrom}, To: {DateTo})",
            SelectedWarehouseId, DateFrom, DateTo);

        var result = await ReportApiService.GetWarehouseMovementsAsync(SelectedWarehouseId, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.Date)
                .ToList();

            ReportData = new ObservableCollection<WarehouseMovementReportDto>(sorted);
            HasSearched = true;

            Log.Information("Warehouse movement report loaded: {Count} movements", ReportData.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل حركة المخازن", "WarehouseMovementReportViewModel.LoadDataAsync");
            Log.Warning("Failed to load warehouse movement report: {Error}", result.Error);
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
                    Warehouses.Add(new WarehouseDto(0, "جميع المخازن", null, null, null, true));

                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المستودعات", "WarehouseMovementReportViewModel.LoadWarehousesAsync", ex);
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
                FileName = $"WarehouseMovementReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("حركة المخازن");

                    // Headers
                    worksheet.Cell(1, 1).Value = "التاريخ";
                    worksheet.Cell(1, 2).Value = "المنتج";
                    worksheet.Cell(1, 3).Value = "المستودع";
                    worksheet.Cell(1, 4).Value = "نوع الحركة";
                    worksheet.Cell(1, 5).Value = "الكمية";
                    worksheet.Cell(1, 6).Value = "قبل";
                    worksheet.Cell(1, 7).Value = "بعد";
                    worksheet.Cell(1, 8).Value = "المرجع";

                    // Data
                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.Date.ToString("yyyy/MM/dd HH:mm");
                        worksheet.Cell(i + 2, 2).Value = item.ProductName;
                        worksheet.Cell(i + 2, 3).Value = item.WarehouseName;
                        worksheet.Cell(i + 2, 4).Value = item.MovementType;
                        worksheet.Cell(i + 2, 5).Value = item.QuantityChange;
                        worksheet.Cell(i + 2, 6).Value = item.QuantityBefore;
                        worksheet.Cell(i + 2, 7).Value = item.QuantityAfter;
                        worksheet.Cell(i + 2, 8).Value = item.ReferenceType;
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
            LogSystemError("فشل في تصدير حركة المخازن إلى Excel", "WarehouseMovementReportViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("التاريخ", typeof(string));
            dataTable.Columns.Add("المنتج", typeof(string));
            dataTable.Columns.Add("المستودع", typeof(string));
            dataTable.Columns.Add("نوع الحركة", typeof(string));
            dataTable.Columns.Add("الكمية", typeof(decimal));
            dataTable.Columns.Add("قبل", typeof(decimal));
            dataTable.Columns.Add("بعد", typeof(decimal));
            dataTable.Columns.Add("المرجع", typeof(string));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.Date.ToString("yyyy/MM/dd HH:mm"),
                    item.ProductName, item.WarehouseName,
                    item.MovementType, item.QuantityChange,
                    item.QuantityBefore, item.QuantityAfter,
                    item.ReferenceType);

            await PdfExportService.ExportToPdfAsync("حركة المخازن", dataTable, 0,
                $"WarehouseMovement_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير حركة المخازن إلى PDF", "WarehouseMovementReportViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
