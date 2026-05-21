using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Dashboard ViewModel - loads and displays dashboard statistics
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IEventBus _eventBus;
    private readonly IDashboardApiService _dashboardService;
    private readonly ISalesInvoiceApiService _salesInvoiceService;
    private readonly IProductApiService _productService;
    private readonly IReportApiService _reportService;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;

    private decimal _todaySales;
    private int _todaySalesCount;
    private decimal _todayPurchases;
    private int _lowStockCount;
    private int _totalCustomers;
    private int _totalSuppliers;
    private int _totalProducts;
    private decimal _totalReceivables;
    private decimal _totalPayables;
    private decimal _monthSales;
    private decimal _monthPurchases;
    private decimal _netProfit;
    private bool _lowStockWarningShown;

    public DashboardViewModel()
        : this(
            App.GetService<IEventBus>(),
            App.GetService<IDashboardApiService>(),
            App.GetService<ISalesInvoiceApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IReportApiService>(),
            App.GetService<ISettingsApiService>(),
            App.GetService<IDialogService>())
    {
        SubscribeToEvents();
    }

    public DashboardViewModel(
        IEventBus eventBus,
        IDashboardApiService dashboardService,
        ISalesInvoiceApiService salesInvoiceService,
        IProductApiService productService,
        IReportApiService reportService,
        ISettingsApiService settingsService,
        IDialogService dialogService)
    {
        _eventBus = eventBus;
        _dashboardService = dashboardService;
        _salesInvoiceService = salesInvoiceService;
        _productService = productService;
        _reportService = reportService;
        _settingsService = settingsService;
        _dialogService = dialogService;

        RecentInvoices = new ObservableCollection<RecentInvoiceItem>();
        LowStockItems = new ObservableCollection<LowStockItem>();

        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadDataOperationAsync)));
    }

    #region Properties
    public decimal TodaySales
    {
        get => _todaySales;
        set => SetProperty(ref _todaySales, value);
    }

    public int TodaySalesCount
    {
        get => _todaySalesCount;
        set => SetProperty(ref _todaySalesCount, value);
    }

    public decimal TodayPurchases
    {
        get => _todayPurchases;
        set => SetProperty(ref _todayPurchases, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        set => SetProperty(ref _lowStockCount, value);
    }

    public int TotalCustomers
    {
        get => _totalCustomers;
        set => SetProperty(ref _totalCustomers, value);
    }

    public int TotalSuppliers
    {
        get => _totalSuppliers;
        set => SetProperty(ref _totalSuppliers, value);
    }

    public int TotalProducts
    {
        get => _totalProducts;
        set => SetProperty(ref _totalProducts, value);
    }

    public decimal TotalReceivables
    {
        get => _totalReceivables;
        set => SetProperty(ref _totalReceivables, value);
    }

    public decimal TotalPayables
    {
        get => _totalPayables;
        set => SetProperty(ref _totalPayables, value);
    }

    public decimal MonthSales
    {
        get => _monthSales;
        set => SetProperty(ref _monthSales, value);
    }

    public decimal MonthPurchases
    {
        get => _monthPurchases;
        set => SetProperty(ref _monthPurchases, value);
    }

    public decimal NetProfit
    {
        get => _netProfit;
        set => SetProperty(ref _netProfit, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<RecentInvoiceItem> RecentInvoices { get; }
    public ObservableCollection<LowStockItem> LowStockItems { get; }

    public string FormattedTodaySales => TodaySales.ToString("N2");
    public string FormattedTodayPurchases => TodayPurchases.ToString("N2");
    public string FormattedMonthSales => MonthSales.ToString("N2");
    public string FormattedMonthPurchases => MonthPurchases.ToString("N2");
    public string FormattedNetProfit => NetProfit.ToString("N2");
    public string FormattedReceivables => TotalReceivables.ToString("N2");
    public string FormattedPayables => TotalPayables.ToString("N2");
    #endregion

    #region Commands
    public AsyncRelayCommand RefreshCommand { get; }
    #endregion

    #region EventBus Subscription
    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<SaleInvoiceChangedMessage>(OnSaleInvoiceChanged);
        _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
        _eventBus.Subscribe<StockChangedMessage>(OnStockChanged);
    }

    private void OnSaleInvoiceChanged(SaleInvoiceChangedMessage msg)
    {
        RefreshData();
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        RefreshData();
    }

    private void OnStockChanged(StockChangedMessage msg)
    {
        RefreshData();
    }

    private void RefreshData()
    {
        if (System.Windows.Application.Current?.Dispatcher != null)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Func<Task>)(async () => await ExecuteAsync(LoadDataOperationAsync)));
        }
        else
        {
            _ = ExecuteAsync(LoadDataOperationAsync);
        }
    }
    #endregion

    #region Data Loading
    private async Task LoadDataOperationAsync()
    {
        var summaryResult = await _dashboardService.GetSummaryAsync();
        if (summaryResult.IsSuccess && summaryResult.Value != null)
        {
            var summary = summaryResult.Value;
            TodaySales = summary.TotalSalesToday;
            TodaySalesCount = summary.NumberOfSalesToday;
            TodayPurchases = summary.TotalPurchasesToday;
            LowStockCount = summary.LowStockItemsCount;
            TotalCustomers = summary.ActiveCustomersCount;
            TotalSuppliers = summary.ActiveSuppliersCount;
            TotalProducts = summary.TotalProductsCount;
            TotalReceivables = summary.TotalReceivables;
            TotalPayables = summary.TotalPayables;
            MonthSales = summary.TotalSalesMonth;
            MonthPurchases = summary.TotalPurchasesMonth;
            NetProfit = MonthSales - MonthPurchases;

            OnPropertyChanged(nameof(FormattedTodaySales));
            OnPropertyChanged(nameof(FormattedTodayPurchases));
            OnPropertyChanged(nameof(FormattedMonthSales));
            OnPropertyChanged(nameof(FormattedMonthPurchases));
            OnPropertyChanged(nameof(FormattedNetProfit));
            OnPropertyChanged(nameof(FormattedReceivables));
            OnPropertyChanged(nameof(FormattedPayables));
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(summaryResult.Error ?? "فشل في تحميل ملخص البيانات", "DashboardViewModel.LoadDataAsync", "[DashboardViewModel.LoadDataAsync] Failed to load dashboard summary.");
        }

        var recentSalesResult = await _salesInvoiceService.GetAllAsync(pageSize: 5);
        if (recentSalesResult.IsSuccess && recentSalesResult.Value != null)
        {
            RecentInvoices.Clear();
            foreach (var inv in recentSalesResult.Value.OrderByDescending(x => x.InvoiceDate).Take(5))
            {
                RecentInvoices.Add(new RecentInvoiceItem
                {
                    Id = inv.Id,
                    CustomerName = inv.CustomerName ?? "عميل نقدي",
                    Date = inv.InvoiceDate,
                    Amount = inv.TotalAmount,
                    Status = inv.Status
                });
            }
        }
        else if (!recentSalesResult.IsSuccess)
        {
            HandleFailure(recentSalesResult.Error ?? "فشل في تحميل الفواتير الأخيرة", "DashboardViewModel.LoadDataAsync", "[DashboardViewModel.LoadDataAsync] Failed to load recent invoices.");
        }

        var settingsResult = await _settingsService.GetSettingsAsync();
        bool enableStockAlerts = settingsResult.IsSuccess && settingsResult.Value?.EnableStockAlerts == true;

        if (enableStockAlerts)
        {
            var lowStockResult = await _reportService.GetLowStockReportAsync();
            if (lowStockResult.IsSuccess && lowStockResult.Value != null)
            {
                LowStockItems.Clear();
                foreach (var item in lowStockResult.Value.Take(10))
                {
                    LowStockItems.Add(new LowStockItem
                    {
                        ProductCode = item.ProductCode ?? string.Empty,
                        ProductName = item.ProductName,
                        CurrentStock = item.CurrentRetailQty,
                        MinStock = item.ReorderLevelRetailQty
                    });
                }

                var totalLowStock = lowStockResult.Value.Count();
                if (totalLowStock > 0 && !_lowStockWarningShown)
                {
                    _lowStockWarningShown = true;
                    await _dialogService.ShowWarningAsync("تنبيه المخزون", $"⚠️ تنبيه: يوجد {totalLowStock} منتج في المخزون أقل من حد الطلب!\n\n" +
                        "للاطلاع على التفاصيل قم بزيارة قائمة 'نواقص المخزون' من القائمة الجانبية.");
                }
            }
            else if (!lowStockResult.IsSuccess)
            {
                HandleFailure(lowStockResult.Error ?? "فشل في تحميل الأصناف منخفضة المخزون", "DashboardViewModel.LoadDataAsync", "[DashboardViewModel.LoadDataAsync] Failed to load low stock items.");
            }
        }
        else
        {
            LowStockItems.Clear();
        }
    }
    #endregion

    #region Cleanup
    public void Unsubscribe()
    {
        _eventBus.Unsubscribe<SaleInvoiceChangedMessage>(OnSaleInvoiceChanged);
        _eventBus.Unsubscribe<ProductChangedMessage>(OnProductChanged);
        _eventBus.Unsubscribe<StockChangedMessage>(OnStockChanged);
    }
    #endregion
}

/// <summary>
/// Recent invoice item for display
/// </summary>
public class RecentInvoiceItem
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public byte Status { get; set; }

    public string FormattedAmount => Amount.ToString("N2");
    public string FormattedDate => Date.ToString("yyyy/MM/dd");
}

/// <summary>
/// Low stock item for display
/// </summary>
public class LowStockItem
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }

    public string FormattedCurrentStock => CurrentStock.ToString("N3");
    public string FormattedMinStock => MinStock.ToString("N3");
}
