using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;

namespace SalesSystem.Desktop.Forms;

public partial class WarehouseEditorForm : Form
{
    private readonly IWarehouseApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly IEventBus _eventBus;
    private readonly WarehouseDto? _existingWarehouse;

    public WarehouseEditorForm(
        IWarehouseApiService apiService,
        INotificationService notificationService,
        IEventBus eventBus,
        WarehouseDto? existingWarehouse = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _eventBus = eventBus;
        _existingWarehouse = existingWarehouse;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        
        if (_existingWarehouse != null)
        {
            this.Text = "تعديل مستودع";
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
            Result<WarehouseDto> result;
            if (_existingWarehouse == null)
            {
                var request = new CreateWarehouseRequest(txtName.Text, null, txtLocation.Text, chkIsDefault.Checked);
                result = await _apiService.CreateAsync(request);
            }
            else
            {
                var request = new UpdateWarehouseRequest(txtName.Text, _existingWarehouse.Code, txtLocation.Text, chkIsDefault.Checked, chkIsActive.Checked);
                result = await _apiService.UpdateAsync(_existingWarehouse.Id, request);
            }

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("تم الحفظ بنجاح");
                _eventBus.Publish(new WarehouseChangedMessage(result.Value!.Id));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notificationService.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("حدث خطأ أثناء الحفظ: " + ex.Message);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e) => this.Close();
}
