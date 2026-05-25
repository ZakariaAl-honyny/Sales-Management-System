using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
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
    private IInventoryWriteOffApiService? _writeOffApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseApiService => _warehouseApiService ??= App.GetService<IWarehouseApiService>();
    private IInventoryWriteOffApiService WriteOffApiService => _writeOffApiService ??= App.GetService<IInventoryWriteOffApiService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private int _thresholdDays;
    private ExpiredProductDto? _selectedProduct;
    private decimal _writeOffQuantity;
    private string _writeOffReason = "منتهي الصلاحية";
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

        WriteOffCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(WriteOffAsync)));

        // Load data on initialization
        _ = LoadAsync();
        _ = LoadWarehousesAsync();
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
                _ = LoadAsync(); // Reload when threshold changes
            }
        }
    }

    public ObservableCollection<ExpiredProductDto> Products { get; }

    public ExpiredProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public decimal WriteOffQuantity
    {
        get => _writeOffQuantity;
        set => SetProperty(ref _writeOffQuantity, value);
    }

    public string WriteOffReason
    {
        get => _writeOffReason;
        set => SetProperty(ref _writeOffReason, value);
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
    public AsyncRelayCommand WriteOffCommand { get; }

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
                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);

                    // Select first warehouse by default
                    if (result.Value.Count > 0)
                        SelectedWarehouse = result.Value[0];
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل المستودعات", "ExpiredProductsReportViewModel.LoadWarehousesAsync", ex);
        }
    }

    #endregion

    #region Write-Off

    private async Task WriteOffAsync()
    {
        // Validate
        if (SelectedProduct == null)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار منتج من القائمة");
            return;
        }

        if (WriteOffQuantity <= 0)
        {
            await D.ShowWarningAsync("تنبيه", "الكمية يجب أن تكون أكبر من صفر");
            return;
        }

        if (SelectedWarehouse == null)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار مستودع");
            return;
        }

        if (string.IsNullOrWhiteSpace(WriteOffReason))
        {
            await D.ShowWarningAsync("تنبيه", "يرجى إدخال سبب الإتلاف");
            return;
        }

        // Confirmation
        var confirmed = await D.ShowConfirmationAsync(
            "تأكيد الإتلاف",
            $"هل أنت متأكد من إتلاف {WriteOffQuantity} وحدة من {SelectedProduct.ProductName}؟\n\n" +
            $"السبب: {WriteOffReason}");

        if (!confirmed)
        {
            Log.Information("User cancelled write-off for product {ProductId}", SelectedProduct.ProductId);
            return;
        }

        Log.Information("Executing write-off: Product={ProductId}, Warehouse={WarehouseId}, Quantity={Quantity}, Reason={Reason}",
            SelectedProduct.ProductId, SelectedWarehouse.Id, WriteOffQuantity, WriteOffReason);

        var request = new CreateStockWriteOffRequest(
            SelectedProduct.ProductId,
            SelectedWarehouse.Id,
            WriteOffQuantity,
            WriteOffReason,
            null);

        var result = await WriteOffApiService.WriteOffAsync(request);

        if (result.IsSuccess)
        {
            await D.ShowSuccessAsync("تم بنجاح", "تم ترحيل الإتلاف بنجاح");

            // Reset write-off fields
            WriteOffQuantity = 0;
            WriteOffReason = "منتهي الصلاحية";
            SelectedProduct = null;

            // Refresh the list
            _ = LoadAsync();
        }
        else
        {
            Log.Warning("Write-off failed for product {ProductId}: {Error}", SelectedProduct?.ProductId, result.Error);
            await D.ShowErrorAsync("خطأ في الإتلاف", result.Error ?? "فشل في ترحيل الإتلاف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
