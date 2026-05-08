using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class PaymentsControl : BaseModuleControl
{
    private TabControl _tabs;
    private TabPage _tpCustomers;
    private TabPage _tpSuppliers;

    public PaymentsControl(
        ICustomerPaymentApiService custApi,
        ISupplierPaymentApiService supApi,
        ICustomerApiService customerApi,
        ISupplierApiService supplierApi,
        INotificationService notification)
    {
        InitializeComponent();
        
        var custControl = new CustomerPaymentsControl(custApi, customerApi, notification) { Dock = DockStyle.Fill };
        var supControl = new SupplierPaymentsControl(supApi, supplierApi, notification) { Dock = DockStyle.Fill };
        
        _tpCustomers.Controls.Add(custControl);
        _tpSuppliers.Controls.Add(supControl);
    }

    private void InitializeComponent()
    {
        this._tabs = new TabControl();
        this._tpCustomers = new TabPage("سندات القبض (عملاء)");
        this._tpSuppliers = new TabPage("سندات الصرف (موردين)");
        
        this.SuspendLayout();
        
        _tabs.Dock = DockStyle.Fill;
        _tabs.Controls.Add(_tpCustomers);
        _tabs.Controls.Add(_tpSuppliers);
        
        this.Controls.Add(_tabs);
        this.RightToLeft = RightToLeft.Yes;
        this.ResumeLayout(false);
    }

    protected override void RegisterSubscriptions() { }
}
