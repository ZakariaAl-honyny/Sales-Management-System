using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Forms;

public partial class ProductDialog : Form
{
    private readonly IProductApiService _productApi;
    private readonly ICategoryApiService _categoryApi;
    private readonly IUnitApiService _unitApi;
    private readonly INotificationService _notification;
    private readonly ProductDto? _existingProduct;

    public ProductDialog(
        IProductApiService productApi,
        ICategoryApiService categoryApi,
        IUnitApiService unitApi,
        INotificationService notification,
        ProductDto? existingProduct = null)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
        _unitApi = unitApi;
        _notification = notification;
        _existingProduct = existingProduct;
        
        InitializeComponent();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadDropdowns();

        if (_existingProduct != null)
        {
            this.Text = "تعديل منتج";
            txtName.Text = _existingProduct.Name;
            txtCode.Text = _existingProduct.Code;
            txtBarcode.Text = _existingProduct.Barcode;
            txtPurchasePrice.Text = _existingProduct.PurchasePrice.ToString("F2");
            txtSalePrice.Text = _existingProduct.SalePrice.ToString("F2");
            txtMinStock.Text = _existingProduct.MinStock.ToString("F3");
            txtDescription.Text = _existingProduct.Description;
            chkIsActive.Checked = _existingProduct.IsActive;
            
            if (cmbCategory.DataSource is IEnumerable<CategoryDto> cats)
                cmbCategory.SelectedValue = _existingProduct.CategoryId ?? 0;
            
            if (cmbUnit.DataSource is IEnumerable<UnitDto> units)
                cmbUnit.SelectedValue = _existingProduct.UnitId ?? 0;
                
            chkIsActive.Visible = true;
        }
        else
        {
            this.Text = "إضافة منتج جديد";
            chkIsActive.Visible = false;
        }
    }

    private async Task LoadDropdowns()
    {
        var cats = await _categoryApi.GetAllAsync();
        if (cats.IsSuccess)
        {
            cmbCategory.DataSource = cats.Value;
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";
        }

        var units = await _unitApi.GetAllAsync();
        if (units.IsSuccess)
        {
            cmbUnit.DataSource = units.Value;
            cmbUnit.DisplayMember = "Name";
            cmbUnit.ValueMember = "Id";
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            _notification.ShowWarning("يرجى إدخال اسم المنتج");
            return;
        }

        btnSave.Enabled = false;
        try
        {
            var product = new ProductDto(
                _existingProduct?.Id ?? 0,
                txtCode.Text,
                txtBarcode.Text,
                txtName.Text,
                (int?)cmbCategory.SelectedValue,
                null,
                (int?)cmbUnit.SelectedValue,
                null,
                txtPurchasePrice.DecimalValue,
                txtSalePrice.DecimalValue,
                decimal.TryParse(txtMinStock.Text, out var ms) ? ms : 0m,
                txtDescription.Text,
                chkIsActive.Checked
            );

            Result result = _existingProduct == null 
                ? await _productApi.CreateAsync(product)
                : await _productApi.UpdateAsync(product);

            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم الحفظ بنجاح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e) => this.Close();
}
