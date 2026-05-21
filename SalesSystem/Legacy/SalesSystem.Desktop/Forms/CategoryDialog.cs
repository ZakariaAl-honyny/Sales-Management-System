using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class CategoryDialog : Form
{
    private readonly ICategoryApiService _apiService;
    private readonly INotificationService _notificationService;
    private CategoryDto? _existingCategory;

    public CategoryDialog(
        ICategoryApiService apiService,
        INotificationService notificationService)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    public void LoadData(CategoryDto? existingCategory)
    {
        _existingCategory = existingCategory;
        
        if (_existingCategory != null)
        {
            this.Text = "تعديل تصنيف";
            txtName.Text = _existingCategory.Name;
            txtDescription.Text = _existingCategory.Description;
            chkIsActive.Checked = _existingCategory.IsActive;
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة تصنيف جديد";
            txtName.Clear();
            txtDescription.Clear();
            chkIsActive.Visible = false;
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notificationService.ShowWarning("يرجى إدخال اسم التصنيف");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            if (_existingCategory == null)
            {
                var request = new CreateCategoryRequest(txtName.Text, txtDescription.Text);
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
                var request = new UpdateCategoryRequest(txtName.Text, txtDescription.Text, chkIsActive.Checked);
                var result = await _apiService.UpdateAsync(_existingCategory.Id, request);
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
            Log.Error(ex, "حدث خطأ في حفظ بيانات التصنيف");
            _notificationService.ShowError("حدث خطأ أثناء الحفظ. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e) => this.Close();
}

