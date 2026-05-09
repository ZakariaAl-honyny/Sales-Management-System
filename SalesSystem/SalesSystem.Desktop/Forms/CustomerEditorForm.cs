using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Messaging.Messages;

namespace SalesSystem.Desktop.Forms;

public partial class CustomerEditorForm : Form
{
    private readonly ICustomerApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly IEventBus _eventBus;
    private readonly CustomerDto? _existingCustomer;

    public CustomerEditorForm(
        ICustomerApiService apiService,
        INotificationService notificationService,
        IEventBus eventBus,
        CustomerDto? existingCustomer = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _eventBus = eventBus;
        _existingCustomer = existingCustomer;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        
        if (_existingCustomer != null)
        {
            this.Text = "تعديل عميل";
            txtName.Text = _existingCustomer.Name;
            txtPhone.Text = _existingCustomer.Phone;
            txtEmail.Text = _existingCustomer.Email;
            txtAddress.Text = _existingCustomer.Address;
            txtOpeningBalance.Text = _existingCustomer.OpeningBalance.ToString("F2");
            txtOpeningBalance.Enabled = false; 
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
                var request = new CreateCustomerRequest(
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
                    _eventBus.Publish(new CustomerChangedMessage(result.Value!.Id));
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else _notificationService.ShowError(result.Error!);
            }
            else
            {
                var request = new UpdateCustomerRequest(
                    txtName.Text, 
                    _existingCustomer.Code,
                    txtPhone.Text, 
                    txtEmail.Text, 
                    txtAddress.Text, 
                    _existingCustomer.CreditLimit,
                    chkIsActive.Checked
                );
                var result = await _apiService.UpdateAsync(_existingCustomer.Id, request);
                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess("تم التعديل بنجاح");
                    _eventBus.Publish(new CustomerChangedMessage(_existingCustomer.Id));
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
