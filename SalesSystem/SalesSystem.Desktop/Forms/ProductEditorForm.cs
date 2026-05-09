using SalesSystem.Desktop.Forms;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Controls.Categories;
using SalesSystem.Desktop.Controls.Units;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class ProductEditorForm : Form
{
    private readonly IProductApiService _productApi;
    private readonly ICategoryApiService _categoryApi;
    private readonly IUnitApiService _unitApi;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notification;
    private readonly IServiceProvider _serviceProvider;
    private readonly int? _editId;

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
        IServiceProvider serviceProvider,
        int? editId = null)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
        _unitApi = unitApi;
        _eventBus = eventBus;
        _notification = notification;
        _serviceProvider = serviceProvider;
        _editId = editId;

        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = _editId.HasValue ? "طھط¹ط¯ظٹظ„ ظ…ظ†طھط¬" : "ط¥ط¶ط§ظپط© ظ…ظ†طھط¬ ط¬ط¯ظٹط¯";
        this.Size = new Size(500, 600);
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

    private async Task LoadProductAsync(int id)
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

    private async void btnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCode.Text))
        {
            _notification.ShowWarning("ظٹط±ط¬ظ‰ ط¥ط¯ط®ط§ظ„ ظƒظˆط¯ ظˆط§ط³ظ… ط§ظ„ظ…ظ†طھط¬");
            return;
        }

        if (cmbCategory.SelectedValue is not int categoryId || cmbUnit.SelectedValue is not int unitId)
        {
            _notification.ShowWarning("ظٹط±ط¬ظ‰ ط§ط®طھظٹط§ط± ط§ظ„طھطµظ†ظٹظپ ظˆط§ظ„ظˆط­ط¯ط©");
            return;
        }

        Result<ProductDto> result;
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
            _notification.ShowSuccess("طھظ… ط­ظپط¸ ط§ظ„ظ…ظ†طھط¬ ط¨ظ†ط¬ط§ط­");
            _eventBus.Publish(new ProductChangedMessage(result.Value!.Id));
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private void btnAddCategory_Click(object? sender, EventArgs e)
    {
        using var dialog = ActivatorUtilities.CreateInstance<CategoryManagerDialog>(_serviceProvider);
        dialog.ShowDialog();
    }

    private void btnAddUnit_Click(object? sender, EventArgs e)
    {
        using var dialog = ActivatorUtilities.CreateInstance<UnitManagerDialog>(_serviceProvider);
        dialog.ShowDialog();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 11, Padding = new Padding(20) };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;
        AddRow(mainLayout, ref row, "ط·آ§ط¸â€‍ط¸ئ’ط¸ث†ط·آ¯:", txtCode = new TextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "ط·آ§ط¸â€‍ط·آ¨ط·آ§ط·آ±ط¸ئ’ط¸ث†ط·آ¯:", txtBarcode = new TextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "ط·آ§ط·آ³ط¸â€¦ ط·آ§ط¸â€‍ط¸â€¦ط¸â€ ط·ع¾ط·آ¬:", txtName = new TextBox { Dock = DockStyle.Fill });

        var catPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        catPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        catPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
        cmbCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        btnAddCategory = new Button { Text = "+", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        btnAddCategory.Click += btnAddCategory_Click;
        catPanel.Controls.Add(cmbCategory, 0, 0);
        catPanel.Controls.Add(btnAddCategory, 1, 0);
        AddRow(mainLayout, ref row, "ط·آ§ط¸â€‍ط·ع¾ط·آµط¸â€ ط¸ظ¹ط¸ظ¾:", catPanel);

        var unitPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        unitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        unitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
        cmbUnit = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        btnAddUnit = new Button { Text = "+", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        btnAddUnit.Click += btnAddUnit_Click;
        unitPanel.Controls.Add(cmbUnit, 0, 0);
        unitPanel.Controls.Add(btnAddUnit, 1, 0);
        AddRow(mainLayout, ref row, "ط·آ§ط¸â€‍ط¸ث†ط·آ­ط·آ¯ط·آ©:", unitPanel);

        AddRow(mainLayout, ref row, "ط·آ³ط·آ¹ط·آ± ط·آ§ط¸â€‍ط·آ´ط·آ±ط·آ§ط·طŒ:", numPurchasePrice = new MoneyTextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "ط·آ³ط·آ¹ط·آ± ط·آ§ط¸â€‍ط·آ¨ط¸ظ¹ط·آ¹:", numSalePrice = new MoneyTextBox { Dock = DockStyle.Fill });
        AddRow(mainLayout, ref row, "ط·آ­ط·آ¯ ط·آ§ط¸â€‍ط·آ·ط¸â€‍ط·آ¨:", numMinStock = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 3, Maximum = 1000000 });
        AddRow(mainLayout, ref row, "ط·آ§ط¸â€‍ط¸ث†ط·آµط¸ظ¾:", txtDescription = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 });

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnSave = new Button { Text = "ط·آ­ط¸ظ¾ط·آ¸", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.LightBlue };
        btnSave.Click += btnSave_Click;
        btnCancel = new Button { Text = "ط·آ¥ط¸â€‍ط·ط›ط·آ§ط·طŒ", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat };
        btnCancel.Click += (_, _) => this.Close();
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);
        mainLayout.Controls.Add(buttonPanel, 1, 10);

        this.Controls.Add(mainLayout);
    }

    private void AddRow(TableLayoutPanel panel, ref int row, string labelText, Control control)
    {
        panel.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
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


