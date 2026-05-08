using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Desktop.Forms;

public partial class SupplierPaymentDialog : Form
{
    private readonly ISupplierPaymentApiService _paymentApi;
    private readonly ISupplierApiService _supplierApi;
    private readonly INotificationService _notification;

    public SupplierPaymentDialog(
        ISupplierPaymentApiService paymentApi,
        ISupplierApiService supplierApi,
        INotificationService notification)
    {
        _paymentApi = paymentApi;
        _supplierApi = supplierApi;
        _notification = notification;
        InitializeComponent();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var suppliers = await _supplierApi.GetAllAsync();
        if (suppliers.IsSuccess)
        {
            cmbSupplier.DataSource = suppliers.Value;
            cmbSupplier.DisplayMember = "Name";
            cmbSupplier.ValueMember = "Id";
        }
        
        cmbMethod.DataSource = Enum.GetValues(typeof(PaymentType));
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (cmbSupplier.SelectedValue == null) { _notification.ShowWarning("يرجى اختيار المورد"); return; }
        if (numAmount.Value <= 0) { _notification.ShowWarning("المبلغ يجب أن يكون أكبر من صفر"); return; }

        var request = new CreateSupplierPaymentRequest(
            (int)cmbSupplier.SelectedValue,
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
