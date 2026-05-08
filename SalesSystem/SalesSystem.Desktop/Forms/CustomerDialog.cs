using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Forms;

public partial class CustomerDialog : Form
{
    private readonly ICustomerApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly CustomerDto? _existingCustomer;

    public CustomerDialog(
        ICustomerApiService apiService,
        INotificationService notificationService,
        CustomerDto? existingCustomer = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _existingCustomer = existingCustomer;
        
        InitializeComponent();
        
        if (_existingCustomer != null)
        {
            this.Text = "تعديل بيانات عميل";
            txtName.Text = _existingCustomer.Name;
            txtPhone.Text = _existingCustomer.Phone;
            txtEmail.Text = _existingCustomer.Email;
            txtAddress.Text = _existingCustomer.Address;
            txtOpeningBalance.Text = _existingCustomer.CurrentBalance.ToString("F2");
            txtOpeningBalance.Enabled = false; // Cannot edit opening balance after creation
            chkIsActive.Checked = _existingCustomer.IsActive;
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة عميل جديد";
            chkIsActive.Visible = false;
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notificationService.ShowWarning("يرجى إدخال اسم العميل");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            if (_existingCustomer == null)
            {
                var result = await _apiService.CreateAsync(txtName.Text, txtPhone.Text, txtEmail.Text, txtAddress.Text, txtOpeningBalance.DecimalValue);
                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("تمت الإضافة بنجاح");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else _notificationService.ShowError(result.Error!);
            }
            else
            {
                var result = await _apiService.UpdateAsync(_existingCustomer.Id, txtName.Text, txtPhone.Text, txtEmail.Text, txtAddress.Text, chkIsActive.Checked);
                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("تم التعديل بنجاح");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else _notificationService.ShowError(result.Error!);
            }
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e) => this.Close();
}
