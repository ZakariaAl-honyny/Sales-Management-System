using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Messaging.Messages;

namespace SalesSystem.Desktop.Forms;

public partial class SupplierEditorForm : Form
{
    private readonly ISupplierApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly IEventBus _eventBus;
    private readonly SupplierDto? _existingSupplier;

    public SupplierEditorForm(
        ISupplierApiService apiService,
        INotificationService notificationService,
        IEventBus eventBus,
        SupplierDto? existingSupplier = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _eventBus = eventBus;
        _existingSupplier = existingSupplier;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        
        if (_existingSupplier != null)
        {
            this.Text = "تعديل مورد";
            txtName.Text = _existingSupplier.Name;
            txtPhone.Text = _existingSupplier.Phone;
            txtEmail.Text = _existingSupplier.Email;
            txtAddress.Text = _existingSupplier.Address;
            txtOpeningBalance.Text = _existingSupplier.OpeningBalance.ToString("F2");
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
                var request = new CreateSupplierRequest(
                    txtName.Text,
                    null,
                    txtPhone.Text,
                    txtEmail.Text,
                    txtAddress.Text,
                    txtOpeningBalance.DecimalValue
                );
                var result = await _apiService.CreateAsync(request);
                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("تمت الإضافة بنجاح");
                    _eventBus.Publish(new SupplierChangedMessage(result.Value!.Id));
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else _notificationService.ShowError(result.Error!);
            }
            else
            {
                var request = new UpdateSupplierRequest(
                    txtName.Text,
                    _existingSupplier.Code,
                    txtPhone.Text,
                    txtEmail.Text,
                    txtAddress.Text,
                    chkIsActive.Checked
                );
                var result = await _apiService.UpdateAsync(_existingSupplier.Id, request);
                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("تم التعديل بنجاح");
                    _eventBus.Publish(new SupplierChangedMessage(_existingSupplier.Id));
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
