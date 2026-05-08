using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class WarehouseDialog : Form
{
    private readonly IWarehouseApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly WarehouseDto? _existingWarehouse;

    public WarehouseDialog(
        IWarehouseApiService apiService,
        INotificationService notificationService,
        WarehouseDto? existingWarehouse = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _existingWarehouse = existingWarehouse;
        
        InitializeComponent();
        
        if (_existingWarehouse != null)
        {
            this.Text = "تعديل مستودع";
            txtCode.Text = _existingWarehouse.Code;
            txtName.Text = _existingWarehouse.Name;
            txtLocation.Text = _existingWarehouse.Location;
            chkIsDefault.Checked = _existingWarehouse.IsDefault;
            chkIsActive.Checked = _existingWarehouse.IsActive;
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة مستودع جديد";
            chkIsActive.Visible = false;
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notificationService.ShowWarning("يرجى إدخال اسم المستودع");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            if (_existingWarehouse == null)
            {
                var result = await _apiService.CreateAsync(txtName.Text, txtCode.Text, txtLocation.Text, chkIsDefault.Checked);
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
                var result = await _apiService.UpdateAsync(_existingWarehouse.Id, txtName.Text, txtCode.Text, txtLocation.Text, chkIsDefault.Checked, chkIsActive.Checked);
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
