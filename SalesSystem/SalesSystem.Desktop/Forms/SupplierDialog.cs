using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Forms;

public partial class SupplierDialog : Form
{
    private readonly ISupplierApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly SupplierDto? _existingSupplier;

    public SupplierDialog(
        ISupplierApiService apiService,
        INotificationService notificationService,
        SupplierDto? existingSupplier = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _existingSupplier = existingSupplier;
        
        InitializeComponent();
        
        if (_existingSupplier != null)
        {
            this.Text = "تعديل بيانات مورد";
            txtName.Text = _existingSupplier.Name;
            txtPhone.Text = _existingSupplier.Phone;
            txtEmail.Text = _existingSupplier.Email;
            txtAddress.Text = _existingSupplier.Address;
            txtOpeningBalance.Text = _existingSupplier.CurrentBalance.ToString("F2");
            txtOpeningBalance.Enabled = false;
            chkIsActive.Checked = _existingSupplier.IsActive;
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة مورد جديد";
            chkIsActive.Visible = false;
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notificationService.ShowWarning("يرجى إدخال اسم المورد");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            if (_existingSupplier == null)
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
                var result = await _apiService.UpdateAsync(_existingSupplier.Id, txtName.Text, txtPhone.Text, txtEmail.Text, txtAddress.Text, chkIsActive.Checked);
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
