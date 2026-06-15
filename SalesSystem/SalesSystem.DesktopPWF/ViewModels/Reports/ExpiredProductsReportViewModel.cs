using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Expired Products Report — displays expired/near-expiry products
/// and allows writing off quantities from warehouse stock.
/// </summary>
public class ExpiredProductsReportViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IWarehouseApiService? _warehouseApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseApiService => _warehouseApiService ??= App.GetService<IWarehouseApiService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int _thresholdDays;
    private ExpiredProductDto? _selectedProduct;
    private string? _errorMessage;
    private WarehouseDto? _selectedWarehouse;

    public ExpiredProductsReportViewModel()
    {
        ThresholdDaysOptions = new ObservableCollection<int> { 0, 30, 60, 90 };
        _thresholdDays = 0;

        Products = new ObservableCollection<ExpiredProductDto>();
        Warehouses = new ObservableCollection<WarehouseDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        // Load data on initialization
        _ = ExecuteAsync(LoadAsync);
        _ = ExecuteAsync(LoadWarehousesCoreAsync);

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));
    }

    #region Properties

    public ObservableCollection<int> ThresholdDaysOptions { get; }

    public int ThresholdDays
    {
        get => _thresholdDays;
        set
        {
            if (SetProperty(ref _thresholdDays, value))
            {
                _ = ExecuteAsync(LoadAsync); // Reload when threshold changes
            }
        }
    }

    public ObservableCollection<ExpiredProductDto> Products { get; }

    public ExpiredProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public ObservableCollection<WarehouseDto> Warehouses { get; }

    public WarehouseDto? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set => SetProperty(ref _selectedWarehouse, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// True when there are products in the list
    /// </summary>
    public bool HasProducts => Products.Count > 0;

    /// <summary>
    /// True when there are no products — show empty state
    /// </summary>
    public bool IsEmpty => Products.Count == 0;

    #endregion

    #region Commands

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading expired products report with threshold: {ThresholdDays} days", ThresholdDays);

        var result = await ReportApiService.GetExpiredProductsReportAsync(ThresholdDays);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Products.Clear();

                foreach (var p in result.Value!
                    .OrderByDescending(x => x.ExpirationDate))
                {
                    Products.Add(p);
                }

                OnPropertyChanged(nameof(HasProducts));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Loaded {Count} expired products", Products.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل البيانات", "ExpiredProductsReportViewModel.LoadAsync");
            Log.Warning("Failed to load expired products: {Error}", result.Error);
        }
    }

    private async Task LoadWarehousesCoreAsync()
    {
        var result = await WarehouseApiService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Warehouses.Clear();
                foreach (var wh in result.Value)
                    Warehouses.Add(wh);

                // Select first warehouse by default
                if (result.Value.Count > 0)
                    SelectedWarehouse = result.Value[0];
            });
        }
        else
        {
            HandleFailure(result.Error ?? "فشل في تحميل المستودعات", "ExpiredProductsReportViewModel.LoadWarehousesCoreAsync");
        }
    }

    #endregion

    #region Export

    private async Task ExportPdfAsync()
    {
        if (Products.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("المنتج", typeof(string));
            dataTable.Columns.Add("التصنيف", typeof(string));
            dataTable.Columns.Add("تاريخ الانتهاء", typeof(string));
            dataTable.Columns.Add("أيام منتهي", typeof(int));
            dataTable.Columns.Add("المخزون الحالي", typeof(decimal));
            dataTable.Columns.Add("الحالة", typeof(string));

            foreach (var item in Products)
            {
                var status = item.DaysExpired > 0 ? "منتهي" : "ينتهي قريباً";
                dataTable.Rows.Add(item.ProductName, item.CategoryName,
                    item.ExpirationDate.ToString("yyyy/MM/dd"),
                    item.DaysExpired, item.CurrentStock, status);
            }

            await PdfExportService.ExportToPdfAsync("المنتجات منتهية الصلاحية", dataTable, 0,
                $"ExpiredProducts_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المنتجات منتهية الصلاحية إلى PDF", "ExpiredProductsReportViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
