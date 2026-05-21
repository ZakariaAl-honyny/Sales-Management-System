using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class CustomerPaymentDialog : Form
{
    private readonly ICustomerPaymentApiService _paymentApi;
    private readonly ICustomerApiService _customerApi;
    private readonly ISalesInvoiceApiService _salesApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    
    private List<CustomerDto> _customers = new();
    private decimal _currentBalance = 0;

    public CustomerPaymentDialog(
        ICustomerPaymentApiService paymentApi,
        ICustomerApiService customerApi,
        ISalesInvoiceApiService salesApi,
        INotificationService notification,
        IEventBus eventBus)
    {
        _paymentApi = paymentApi;
        _customerApi = customerApi;
        _salesApi = salesApi;
        _notification = notification;
        _eventBus = eventBus;

        InitializeComponent();
        
        btnSave.Click += async (s, e) => await SaveAsync();
        btnCancel.Click += (s, e) => this.Close();
        cmbCustomer.SelectedIndexChanged += async (s, e) => await OnCustomerChanged();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            var res = await _customerApi.GetAllAsync();
            if (res.IsSuccess)
            {
                _customers = res.Value?.ToList() ?? new List<CustomerDto>();
                cmbCustomer.DataSource = _customers;
                cmbCustomer.DisplayMember = "Name";
                cmbCustomer.ValueMember = "Id";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل قائمة العملاء في سند قبض");
            _notification.ShowError("حدث خطأ في تحميل البيانات.");
        }
    }

    private async Task OnCustomerChanged()
    {
        try
        {
            if (cmbCustomer.SelectedValue is not int customerId) return;
            var customer = _customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                _currentBalance = customer.CurrentBalance;
                lblBalanceVal.Text = _currentBalance.ToString("N2");
                lblBalanceVal.ForeColor = _currentBalance > 0 ? Color.Red : Color.Blue;
            }

            // Load unpaid invoices
            var invoicesRes = await _salesApi.GetAllAsync(status: 2); // Posted only
            if (invoicesRes.IsSuccess)
            {
                var list = invoicesRes.Value.Where(i => i.CustomerId == customerId && i.DueAmount > 0).ToList();
                cmbInvoice.DataSource = list;
                cmbInvoice.DisplayMember = "InvoiceNo";
                cmbInvoice.ValueMember = "Id";
                cmbInvoice.SelectedIndex = -1;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تغيير العميل في سند قبض");
        }
    }

    private async Task SaveAsync()
    {
        if (cmbCustomer.SelectedValue is not int customerId) return;
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
            var request = new CreateCustomerPaymentRequest(customerId, amount, SalesSystem.Contracts.Enums.PaymentType.Cash, dtpDate.Value, invoiceId, txtNotes.Text);

            var res = await _paymentApi.CreateAsync(request);
            if (res.IsSuccess)
            {
                _notification.ShowSuccess("تم حفظ السداد بنجاح");
                _eventBus.Publish(new CustomerPaymentChangedMessage(res.Value.Id));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في حفظ سند القبض");
            _notification.ShowError("حدث خطأ أثناء الحفظ. تم تسجيل التفاصيل للدعم الفني.");
        }
    }
}
