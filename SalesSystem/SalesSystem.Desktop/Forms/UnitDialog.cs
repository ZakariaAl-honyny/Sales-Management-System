using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

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
                var result = await _apiService.CreateAsync(txtName.Text, txtSymbol.Text);
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
                var result = await _apiService.UpdateAsync(_existingUnit.Id, txtName.Text, txtSymbol.Text, chkIsActive.Checked);
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
