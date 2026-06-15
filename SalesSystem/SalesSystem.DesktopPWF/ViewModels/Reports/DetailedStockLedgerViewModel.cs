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

public class DetailedStockLedgerViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IWarehouseApiService? _warehouseApiService;
    private IProductApiService? _productApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseApiService => _warehouseApiService ??= App.GetService<IWarehouseApiService>();
    private IProductApiService ProductApiService => _productApiService ??= App.GetService<IProductApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedProductId;
    private int? _selectedWarehouseId;
    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<DetailedStockLedgerDto> _reportData = new();

    public DetailedStockLedgerViewModel()
    {
        _dateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _dateTo = DateTime.Today;

        Products = new ObservableCollection<ProductDto>();
        Warehouses = new ObservableCollection<WarehouseDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = LoadProductsAsync();
        _ = LoadWarehousesAsync();
        _ = LoadDataAsync();
    }

    #region Properties

    public ObservableCollection<ProductDto> Products { get; }
    public ObservableCollection<WarehouseDto> Warehouses { get; }

    public int? SelectedProductId
    {
        get => _selectedProductId;
        set => SetProperty(ref _selectedProductId, value);
    }

    public int? SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetProperty(ref _selectedWarehouseId, value);
    }

    public DateTime DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
                _ = LoadDataAsync();
        }
    }

    public ObservableCollection<DetailedStockLedgerDto> ReportData
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

    public int TotalMovements => ReportData.Count;
    public string SummaryText => $"إجمالي الحركات: {TotalMovements}";

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

        Log.Information("Loading detailed stock ledger (ProductId: {ProductId}, WarehouseId: {WarehouseId}, From: {DateFrom}, To: {DateTo})",
            SelectedProductId, SelectedWarehouseId, DateFrom, DateTo);

        var result = await ReportApiService.GetDetailedStockLedgerAsync(SelectedProductId, SelectedWarehouseId, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.Date)
                .ToList();

            ReportData = new ObservableCollection<DetailedStockLedgerDto>(sorted);
            HasSearched = true;

            Log.Information("Detailed stock ledger loaded: {Count} movements", ReportData.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل كشف حركة الأصناف", "DetailedStockLedgerViewModel.LoadDataAsync");
            Log.Warning("Failed to load detailed stock ledger: {Error}", result.Error);
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var result = await ProductApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Products.Clear();
                    Products.Add(new ProductDto(0, "جميع المنتجات", 0, null, null, null, 0, false, null, null, null, null, true));
                    foreach (var p in result.Value)
                        Products.Add(p);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المنتجات", "DetailedStockLedgerViewModel.LoadProductsAsync", ex);
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
                    Warehouses.Add(new WarehouseDto(0, string.Empty, "جميع المخازن", (byte)1, null, null, null, null, true));
                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المستودعات", "DetailedStockLedgerViewModel.LoadWarehousesAsync", ex);
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
                FileName = $"DetailedStockLedger_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("حركة الأصناف");

                    worksheet.Cell(1, 1).Value = "التاريخ";
                    worksheet.Cell(1, 2).Value = "رقم المرجع";
                    worksheet.Cell(1, 3).Value = "نوع المرجع";
                    worksheet.Cell(1, 4).Value = "نوع الحركة";
                    worksheet.Cell(1, 5).Value = "الكمية قبل";
                    worksheet.Cell(1, 6).Value = "الكمية المتغيرة";
                    worksheet.Cell(1, 7).Value = "الكمية بعد";
                    worksheet.Cell(1, 8).Value = "تكلفة الوحدة";
                    worksheet.Cell(1, 9).Value = "التكلفة الإجمالية";
                    worksheet.Cell(1, 10).Value = "بواسطة";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.Date.ToString("yyyy/MM/dd HH:mm");
                        worksheet.Cell(i + 2, 2).Value = item.ReferenceNo;
                        worksheet.Cell(i + 2, 3).Value = item.ReferenceType;
                        worksheet.Cell(i + 2, 4).Value = item.MovementType;
                        worksheet.Cell(i + 2, 5).Value = item.QuantityBefore;
                        worksheet.Cell(i + 2, 6).Value = item.QuantityChange;
                        worksheet.Cell(i + 2, 7).Value = item.QuantityAfter;
                        worksheet.Cell(i + 2, 8).Value = item.UnitCost;
                        worksheet.Cell(i + 2, 9).Value = item.TotalCost;
                        worksheet.Cell(i + 2, 10).Value = item.CreatedBy;
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 10);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير كشف حركة الأصناف إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف حركة الأصناف إلى Excel", "DetailedStockLedgerViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("رقم المرجع", typeof(string));
            dataTable.Columns.Add("نوع المرجع", typeof(string));
            dataTable.Columns.Add("نوع الحركة", typeof(string));
            dataTable.Columns.Add("الكمية قبل", typeof(decimal));
            dataTable.Columns.Add("الكمية المتغيرة", typeof(decimal));
            dataTable.Columns.Add("الكمية بعد", typeof(decimal));
            dataTable.Columns.Add("تكلفة الوحدة", typeof(decimal));
            dataTable.Columns.Add("التكلفة الإجمالية", typeof(decimal));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.Date.ToString("yyyy/MM/dd HH:mm"),
                    item.ReferenceNo, item.ReferenceType, item.MovementType,
                    item.QuantityBefore, item.QuantityChange, item.QuantityAfter,
                    item.UnitCost, item.TotalCost);

            await PdfExportService.ExportToPdfAsync("كشف حركة الأصناف", dataTable, 0,
                $"DetailedStockLedger_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف حركة الأصناف إلى PDF", "DetailedStockLedgerViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
