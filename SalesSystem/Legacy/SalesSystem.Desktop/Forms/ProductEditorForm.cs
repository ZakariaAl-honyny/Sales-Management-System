using Serilog;
using SalesSystem.Desktop.Forms;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Controls.Units;
using SalesSystem.Desktop.Controls.Categories;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

[System.ComponentModel.DesignerCategory("Code")]
public partial class ProductEditorForm : Form
{
    private readonly IProductApiService _productApi;
    private readonly ICategoryApiService _categoryApi;
    private readonly IUnitApiService _unitApi;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notification;
    private readonly IServiceProvider _serviceProvider;
    private int? _editId;

    private TextBox txtCode = null!;
    private TextBox txtBarcode = null!;
    private TextBox txtName = null!;
    private ComboBox cmbCategory = null!;
    private ComboBox cmbUnit = null!;
    private MoneyTextBox numPurchasePrice = null!;
    private MoneyTextBox numSalePrice = null!;
    private NumericUpDown numMinStock = null!;
    private TextBox txtDescription = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Button btnAddCategory = null!;
    private Button btnAddUnit = null!;

    private IDisposable? _catSubscription;
    private IDisposable? _unitSubscription;

    public ProductEditorForm(
        IProductApiService productApi,
        ICategoryApiService categoryApi,
        IUnitApiService unitApi,
        IEventBus eventBus,
        INotificationService notification,
        IServiceProvider serviceProvider)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
        _unitApi = unitApi;
        _eventBus = eventBus;
        _notification = notification;
        _serviceProvider = serviceProvider;

        InitializeComponent();
    }

    public void LoadData(int? editId)
    {
        _editId = editId;
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = _editId.HasValue ? "تعديل منتج" : "إضافة منتج جديد";
        this.Size = new Size(500, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadCombosAsync();
        
        if (_editId.HasValue)
        {
            await LoadProductAsync(_editId.Value);
        }

        _catSubscription = _eventBus.Subscribe<CategoryChangedMessage>(async _ => await LoadCombosAsync());
        _unitSubscription = _eventBus.Subscribe<UnitChangedMessage>(async _ => await LoadCombosAsync());
    }

    private async Task LoadCombosAsync()
    {
        try
        {
            var cats = await _categoryApi.GetAllAsync();
            if (cats.IsSuccess)
            {
                var currentVal = cmbCategory.SelectedValue;
                cmbCategory.DataSource = cats.Value;
                cmbCategory.DisplayMember = "Name";
                cmbCategory.ValueMember = "Id";
                if (currentVal != null) cmbCategory.SelectedValue = currentVal;
            }

            var units = await _unitApi.GetAllAsync();
            if (units.IsSuccess)
            {
                var currentVal = cmbUnit.SelectedValue;
                cmbUnit.DataSource = units.Value;
                cmbUnit.DisplayMember = "Name";
                cmbUnit.ValueMember = "Id";
                if (currentVal != null) cmbUnit.SelectedValue = currentVal;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل القوائم المنسدلة للمنتج");
            _notification.ShowError("حدث خطأ في تحميل البيانات الأساسية.");
        }
    }

    private async Task LoadProductAsync(int id)
    {
        try
        {
            var result = await _productApi.GetByIdAsync(id);
            if (result.IsSuccess)
            {
                var p = result.Value;
                txtCode.Text = p.Code ?? "";
                txtBarcode.Text = p.Barcode ?? "";
                txtName.Text = p.Name;
                cmbCategory.SelectedValue = p.CategoryId;
                cmbUnit.SelectedValue = p.UnitId;
                numPurchasePrice.DecimalValue = p.PurchasePrice;
                numSalePrice.DecimalValue = p.SalePrice;
                numMinStock.Value = p.MinStock;
                txtDescription.Text = p.Description ?? "";
            }
            else
            {
                _notification.ShowError(result.Error!);
                this.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل بيانات المنتج المعرف {ProductId}", id);
            _notification.ShowError("حدث خطأ في تحميل بيانات المنتج.");
            this.Close();
        }
    }

    private async void btnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCode.Text))
        {
            _notification.ShowWarning("يرجى إدخال كود واسم المنتج");
            return;
        }

        if (cmbCategory.SelectedValue is not int categoryId || cmbUnit.SelectedValue is not int unitId)
        {
            _notification.ShowWarning("يرجى اختيار التصنيف والوحدة");
            return;
        }

        Result<ProductDto> result;
        try
        {
            if (_editId.HasValue)
            {
                var updateRequest = new UpdateProductRequest(
                    txtCode.Text,
                    txtBarcode.Text,
                    txtName.Text,
                    categoryId,
                    unitId,
                    numPurchasePrice.DecimalValue,
                    numSalePrice.DecimalValue,
                    numMinStock.Value,
                    txtDescription.Text,
                    true
                );
                result = await _productApi.UpdateAsync(_editId.Value, updateRequest);
            }
            else
            {
                var createRequest = new CreateProductRequest(
                    txtCode.Text,
                    txtBarcode.Text,
                    txtName.Text,
                    categoryId,
                    unitId,
                    numPurchasePrice.DecimalValue,
                    numSalePrice.DecimalValue,
                    numMinStock.Value,
                    txtDescription.Text
                );
                result = await _productApi.CreateAsync(createRequest);
            }

            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم حفظ المنتج بنجاح");
                _eventBus.Publish(new ProductChangedMessage(result.Value!.Id));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ أثناء حفظ المنتج");
            _notification.ShowError("حدث خطأ أثناء الحفظ. تم تسجيل التفاصيل للدعم الفني.");
        }
    }

    private void btnAddCategory_Click(object? sender, EventArgs e)
    {
        using var dialog = _serviceProvider.GetRequiredService<CategoryManagerDialog>();
        dialog.ShowDialog();
    }

    private void btnAddUnit_Click(object? sender, EventArgs e)
    {
        using var dialog = _serviceProvider.GetRequiredService<UnitManagerDialog>();
        dialog.ShowDialog();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 11, Padding = new Padding(20) };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;
        AddRow(mainLayout, ref row, "الكود:", txtCode = new TextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "الباركود:", txtBarcode = new TextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "اسم المنتج:", txtName = new TextBox { Dock = DockStyle.Fill });

        var catPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Height = 35 };
        catPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        catPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
        cmbCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 35 };
        btnAddCategory = new Button { Text = "+", Dock = DockStyle.Fill };
        ThemeHelper.ApplyButtonStyle(btnAddCategory, ThemeHelper.ButtonType.Primary);
        btnAddCategory.Click += btnAddCategory_Click;
        catPanel.Controls.Add(cmbCategory, 0, 0);
        catPanel.Controls.Add(btnAddCategory, 1, 0);
        AddRow(mainLayout, ref row, "التصنيف:", catPanel);

        var unitPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Height = 35 };
        unitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        unitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
        cmbUnit = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 35 };
        btnAddUnit = new Button { Text = "+", Dock = DockStyle.Fill };
        ThemeHelper.ApplyButtonStyle(btnAddUnit, ThemeHelper.ButtonType.Primary);
        btnAddUnit.Click += btnAddUnit_Click;
        unitPanel.Controls.Add(cmbUnit, 0, 0);
        unitPanel.Controls.Add(btnAddUnit, 1, 0);
        AddRow(mainLayout, ref row, "الوحدة:", unitPanel);

        AddRow(mainLayout, ref row, "سعر الشراء:", numPurchasePrice = new MoneyTextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "سعر البيع:", numSalePrice = new MoneyTextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "حد الطلب:", numMinStock = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 3, Maximum = 1000000 });
        AddRow(mainLayout, ref row, "الوصف:", txtDescription = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 });

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 15, 0, 0) };
        btnSave = new Button { Text = "حفظ" };
        ThemeHelper.ApplyButtonStyle(btnSave, ThemeHelper.ButtonType.Success);
        btnSave.Click += btnSave_Click;
        
        btnCancel = new Button { Text = "إلغاء" };
        ThemeHelper.ApplyButtonStyle(btnCancel, ThemeHelper.ButtonType.Neutral);
        btnCancel.Click += (_, _) => this.Close();
        
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);
        mainLayout.Controls.Add(buttonPanel, 1, 10);

        this.Controls.Add(mainLayout);
    }

    private void AddRow(TableLayoutPanel panel, ref int row, string labelText, Control control)
    {
        panel.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) }, 0, row);
        panel.Controls.Add(control, 1, row);
        row++;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _catSubscription?.Dispose();
            _unitSubscription?.Dispose();
        }
        base.Dispose(disposing);
    }
}
