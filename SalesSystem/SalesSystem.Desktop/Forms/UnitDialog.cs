using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class UnitDialog : Form
{
    private readonly IUnitApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly UnitDto? _existingUnit;

    public UnitDialog(
        IUnitApiService apiService,
        INotificationService notificationService,
        UnitDto? existingUnit = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _existingUnit = existingUnit;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        
        if (_existingUnit != null)
        {
            this.Text = "تعديل وحدة";
            txtName.Text = _existingUnit.Name;
            txtSymbol.Text = _existingUnit.Symbol;
            chkIsActive.Checked = _existingUnit.IsActive;
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة وحدة جديدة";
            chkIsActive.Visible = false;
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notificationService.ShowWarning("يرجى إدخال اسم الوحدة");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            if (_existingUnit == null)
            {
                var request = new CreateUnitRequest(txtName.Text, txtSymbol.Text);
                var result = await _apiService.CreateAsync(request);
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
                var request = new UpdateUnitRequest(txtName.Text, txtSymbol.Text, chkIsActive.Checked);
                var result = await _apiService.UpdateAsync(_existingUnit.Id, request);
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

