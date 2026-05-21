using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class SupplierPaymentDialog : Form
{
    private readonly ISupplierPaymentApiService _paymentApi;
    private readonly ISupplierApiService _supplierApi;
    private readonly IPurchaseInvoiceApiService _purchaseApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    
    private List<SupplierDto> _suppliers = new();
    private decimal _currentBalance = 0;

    public SupplierPaymentDialog(
        ISupplierPaymentApiService paymentApi,
        ISupplierApiService supplierApi,
        IPurchaseInvoiceApiService purchaseApi,
        INotificationService notification,
        IEventBus eventBus)
    {
        _paymentApi = paymentApi;
        _supplierApi = supplierApi;
        _purchaseApi = purchaseApi;
        _notification = notification;
        _eventBus = eventBus;

        InitializeComponent();
        
        btnSave.Click += async (s, e) => await SaveAsync();
        btnCancel.Click += (s, e) => this.Close();
        cmbSupplier.SelectedIndexChanged += async (s, e) => await OnSupplierChanged();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            var res = await _supplierApi.GetAllAsync();
            if (res.IsSuccess)
            {
                _suppliers = res.Value?.ToList() ?? new List<SupplierDto>();
                cmbSupplier.DataSource = _suppliers;
                cmbSupplier.DisplayMember = "Name";
                cmbSupplier.ValueMember = "Id";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل قائمة الموردين في سند صرف");
            _notification.ShowError("حدث خطأ في تحميل البيانات.");
        }
    }

    private async Task OnSupplierChanged()
    {
        try
        {
            if (cmbSupplier.SelectedValue is not int supplierId) return;
            var supplier = _suppliers.FirstOrDefault(s => s.Id == supplierId);
            if (supplier != null)
            {
                _currentBalance = supplier.CurrentBalance;
                lblBalanceVal.Text = _currentBalance.ToString("N2");
                lblBalanceVal.ForeColor = _currentBalance > 0 ? Color.Red : Color.Blue;
            }

            // Load unpaid invoices
            var invoicesRes = await _purchaseApi.GetAllAsync(status: 2);
            if (invoicesRes.IsSuccess)
            {
                var list = invoicesRes.Value.Where(i => i.SupplierId == supplierId && i.DueAmount > 0).ToList();
                cmbInvoice.DataSource = list;
                cmbInvoice.DisplayMember = "InvoiceNo";
                cmbInvoice.ValueMember = "Id";
                cmbInvoice.SelectedIndex = -1;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تغيير المورد في سند صرف");
        }
    }

    private async Task SaveAsync()
    {
        if (cmbSupplier.SelectedValue is not int supplierId) return;
        decimal amount = numAmount.Value;
        if (amount <= 0) { _notification.ShowWarning("يرجى إدخال المبلغ"); return; }

        if (amount > _currentBalance)
        {
            if (MessageBox.Show("المبلغ المدفوع أكبر من الرصيد. هل تريد المتابعة؟", "تنبيه", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
        }

        try
        {
            int? invoiceId = (int?)cmbInvoice.SelectedValue;
            var request = new CreateSupplierPaymentRequest(supplierId, amount, SalesSystem.Contracts.Enums.PaymentType.Cash, dtpDate.Value, invoiceId, txtNotes.Text);

            var res = await _paymentApi.CreateAsync(request);
            if (res.IsSuccess)
            {
                _notification.ShowSuccess("تم حفظ السداد بنجاح");
                _eventBus.Publish(new SupplierPaymentChangedMessage(res.Value.Id));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في حفظ سند الصرف");
            _notification.ShowError("حدث خطأ أثناء الحفظ. تم تسجيل التفاصيل للدعم الفني.");
        }
    }
}
