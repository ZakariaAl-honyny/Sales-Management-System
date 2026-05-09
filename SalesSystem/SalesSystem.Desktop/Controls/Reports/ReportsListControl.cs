using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Reports.Tabs;

namespace SalesSystem.Desktop.Controls.Reports;

public partial class ReportsListControl : UserControl
{
    private readonly IReportApiService _reportApi;
    private readonly IProductApiService _productApi;
    private readonly INotificationService _notification;
    private TabControl tabControl = null!;

    public ReportsListControl(
        IReportApiService reportApi,
        IProductApiService productApi,
        INotificationService notification)
    {
        _reportApi = reportApi;
        _productApi = productApi;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            RightToLeftLayout = true,
            Padding = new Point(10, 5)
        };

        var salesTab = new TabPage { Text = "تقرير المبيعات" };
        salesTab.Controls.Add(new SalesReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        var purchasesTab = new TabPage { Text = "تقرير المشتريات" };
        purchasesTab.Controls.Add(new PurchasesReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        var inventoryTab = new TabPage { Text = "تقرير المخزون" };
        inventoryTab.Controls.Add(new InventoryReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        var customerBalancesTab = new TabPage { Text = "كشف حساب عملاء" };
        customerBalancesTab.Controls.Add(new CustomerBalanceReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        var supplierBalancesTab = new TabPage { Text = "كشف حساب موردين" };
        supplierBalancesTab.Controls.Add(new SupplierBalanceReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        var productMovementTab = new TabPage { Text = "حركات منتج" };
        productMovementTab.Controls.Add(new ProductMovementReportTab(_reportApi, _productApi, _notification) { Dock = DockStyle.Fill });

        var lowStockTab = new TabPage { Text = "تنبيه المخزون المنخفض" };
        lowStockTab.Controls.Add(new LowStockReportTab(_reportApi, _notification) { Dock = DockStyle.Fill });

        tabControl.TabPages.AddRange(new[]
        {
            salesTab,
            purchasesTab,
            inventoryTab,
            customerBalancesTab,
            supplierBalancesTab,
            productMovementTab,
            lowStockTab
        });

        this.Controls.Add(tabControl);
    }
}



