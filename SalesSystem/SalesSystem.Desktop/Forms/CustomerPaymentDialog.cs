using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Desktop.Forms;

public partial class CustomerPaymentDialog : Form
{
    private readonly ICustomerPaymentApiService _paymentApi;
    private readonly ICustomerApiService _customerApi;
    private readonly INotificationService _notification;

    public CustomerPaymentDialog(
        ICustomerPaymentApiService paymentApi,
        ICustomerApiService customerApi,
        INotificationService notification)
    {
        _paymentApi = paymentApi;
        _customerApi = customerApi;
        _notification = notification;
        InitializeComponent();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var customers = await _customerApi.GetAllAsync();
        if (customers.IsSuccess)
        {
            cmbCustomer.DataSource = customers.Value;
            cmbCustomer.DisplayMember = "Name";
            cmbCustomer.ValueMember = "Id";
        }
        
        cmbMethod.DataSource = Enum.GetValues(typeof(PaymentType));
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (cmbCustomer.SelectedValue == null) { _notification.ShowWarning("يرجى اختيار العميل"); return; }
        if (numAmount.Value <= 0) { _notification.ShowWarning("المبلغ يجب أن يكون أكبر من صفر"); return; }

        var request = new CreateCustomerPaymentRequest(
            (int)cmbCustomer.SelectedValue,
            numAmount.Value,
            (PaymentType)cmbMethod.SelectedItem,
            dtpDate.Value,
            null,
            txtNotes.Text
        );

        btnSave.Enabled = false;
        try
        {
            var result = await _paymentApi.CreateAsync(request);
            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم تسجيل الدفعة بنجاح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        finally { btnSave.Enabled = true; }
    }
}
