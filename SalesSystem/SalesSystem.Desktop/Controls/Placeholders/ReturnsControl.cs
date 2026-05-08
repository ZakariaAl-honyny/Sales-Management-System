using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class ReturnsControl : BaseModuleControl
{
    private TabControl _tabs;
    private TabPage _tpSales;
    private TabPage _tpPurchases;

    public ReturnsControl(
        ISalesReturnApiService salesApi,
        IPurchaseReturnApiService purApi,
        ICustomerApiService customerApi,
        ISupplierApiService supplierApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification)
    {
        InitializeComponent();
        
        var salesControl = new SalesReturnsControl(salesApi, customerApi, productApi, warehouseApi, notification) { Dock = DockStyle.Fill };
        var purchaseControl = new PurchaseReturnsControl(purApi, supplierApi, productApi, warehouseApi, notification) { Dock = DockStyle.Fill };
        
        _tpSales.Controls.Add(salesControl);
        _tpPurchases.Controls.Add(purchaseControl);
    }

    private void InitializeComponent()
    {
        this._tabs = new TabControl();
        this._tpSales = new TabPage("مرتجعات المبيعات");
        this._tpPurchases = new TabPage("مرتجعات المشتريات");
        
        this.SuspendLayout();
        
        _tabs.Dock = DockStyle.Fill;
        _tabs.Controls.Add(_tpSales);
        _tabs.Controls.Add(_tpPurchases);
        
        this.Controls.Add(_tabs);
        this.RightToLeft = RightToLeft.Yes;
        this.ResumeLayout(false);
    }

    protected override void RegisterSubscriptions() { }
}
