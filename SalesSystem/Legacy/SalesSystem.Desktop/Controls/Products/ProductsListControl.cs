using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Products;

[System.ComponentModel.DesignerCategory("Code")]
public partial class ProductsListControl : UserControl
{
    private readonly IProductApiService _productApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

    private SearchBarControl searchBar = null!;
    private CheckBox chkShowInactive = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnToggleActive = null!;
    private Button btnRefresh = null!;
    private DataGridView dgvProducts = null!;
    private Label lblCount = null!;

    public ProductsListControl(
        IProductApiService productApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _productApi = productApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        
        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        dgvProducts.DataSource = _bindingSource;
        dgvProducts.AutoGenerateColumns = false;
        dgvProducts.ReadOnly = true;
        dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvProducts.MultiSelect = false;
        dgvProducts.AllowUserToAddRows = false;
        dgvProducts.RowHeadersVisible = false;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvProducts);

        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "كود المنتج", Width = 100 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم المنتج", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "التصنيف", Width = 120 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitName", HeaderText = "الوحدة", Width = 80 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalePrice", HeaderText = "سعر البيع", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MinStock", HeaderText = "الحد الأدنى", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
        dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 50 });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.RightToLeft = RightToLeft.Yes;
        _subscription = _eventBus.Subscribe<ProductChangedMessage>(async _ => await LoadDataAsync());
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            var result = await _productApi.GetAllAsync(searchBar.SearchText, null, chkShowInactive.Checked); 
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblCount.Text = $"إجمالي عدد المنتجات: {result.Value.Count}";
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل المنتجات");
            _notification.ShowError("خطأ في تحميل المنتجات. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void btnAdd_Click(object? sender, EventArgs e)
    {
        var editor = _serviceProvider.GetRequiredService<ProductEditorForm>();
        editor.LoadData(null);
        editor.ShowDialog();
    }

    private void btnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvProducts.CurrentRow?.DataBoundItem is not ProductDto product) return;
        var editor = _serviceProvider.GetRequiredService<ProductEditorForm>();
        editor.LoadData(product.Id);
        editor.ShowDialog();
    }

    private async void btnToggleActive_Click(object? sender, EventArgs e)
    {
        if (dgvProducts.CurrentRow?.DataBoundItem is not ProductDto product) return;

        var result = product.IsActive 
            ? await _productApi.DeactivateAsync(product.Id) 
            : await _productApi.ReactivateAsync(product.Id);

        if (result.IsSuccess)
        {
            _notification.ShowSuccess("تم تغيير حالة المنتج بنجاح");
            _eventBus.Publish(new ProductChangedMessage(product.Id));
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F)); // Standard Toolbar Height
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var toolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        btnAdd = new Button { Text = "منتج جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += btnAdd_Click;

        btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        btnEdit.Click += btnEdit_Click;

        btnToggleActive = new Button { Text = "تنشيط/تعطيل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnToggleActive, ThemeHelper.ButtonType.Ghost);
        btnToggleActive.Click += btnToggleActive_Click;

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await LoadDataAsync();

        chkShowInactive = new CheckBox { 
            Text = "إظهار غير النشط", 
            AutoSize = true, 
            Margin = new Padding(15, 8, 15, 0), 
            Font = new Font("Segoe UI", 9F) 
        };
        chkShowInactive.CheckedChanged += async (_, _) => await LoadDataAsync();

        searchBar = new SearchBarControl { Width = 300, Height = ThemeHelper.StandardControlHeight };
        searchBar.SearchChanged += async (_, _) => await LoadDataAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnToggleActive, btnRefresh, chkShowInactive, searchBar });
        topPanel.Controls.Add(toolbar);

        dgvProducts = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvProducts);

        lblCount = new Label { 
            Dock = DockStyle.Fill, 
            TextAlign = ContentAlignment.MiddleLeft, 
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0),
            Text = "جاهز",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ThemeHelper.TextSecondary,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvProducts, 0, 1);
        mainLayout.Controls.Add(lblCount, 0, 2);
        
        this.Controls.Add(mainLayout);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
