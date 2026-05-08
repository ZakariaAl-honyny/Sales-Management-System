using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class CategoryDialog : Form
{
    private readonly ICategoryApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly CategoryDto? _existingCategory;

    public CategoryDialog(
        ICategoryApiService apiService,
        INotificationService notificationService,
        CategoryDto? existingCategory = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _existingCategory = existingCategory;
        
        InitializeComponent();
        
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
                var result = await _apiService.CreateAsync(txtName.Text, txtDescription.Text);
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
                var result = await _apiService.UpdateAsync(_existingCategory.Id, txtName.Text, txtDescription.Text, chkIsActive.Checked);
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
