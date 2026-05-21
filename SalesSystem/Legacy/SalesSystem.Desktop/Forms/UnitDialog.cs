using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class UnitDialog : Form
{
    private readonly IUnitApiService _apiService;
    private readonly INotificationService _notificationService;
    private UnitDto? _existingUnit;

    public UnitDialog(
        IUnitApiService apiService,
        INotificationService notificationService)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    public void LoadData(UnitDto? existingUnit)
    {
        _existingUnit = existingUnit;
        
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
            txtName.Clear();
            txtSymbol.Clear();
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
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في حفظ بيانات الوحدة");
            _notificationService.ShowError("حدث خطأ أثناء الحفظ. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e) => this.Close();
}

