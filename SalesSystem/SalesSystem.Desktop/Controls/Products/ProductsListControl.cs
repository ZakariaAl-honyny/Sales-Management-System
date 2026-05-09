using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Products;

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
        this.RightToLeft = RightToLeft.Yes;
        dgvProducts.DataSource = _bindingSource;
        dgvProducts.AutoGenerateColumns = false;
        dgvProducts.ReadOnly = true;
        dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvProducts.MultiSelect = false;
        dgvProducts.AllowUserToAddRows = false;
        dgvProducts.RowHeadersVisible = false;
        dgvProducts.BackgroundColor = Color.White;

        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 100 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "التصنيف", Width = 120 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitName", HeaderText = "الوحدة", Width = 80 });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalePrice", HeaderText = "سعر البيع", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MinStock", HeaderText = "حد الطلب", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
        dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 50 });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
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
                lblCount.Text = $"عدد المنتجات: {result.Value.Count}";
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("حدث خطأ أثناء تحميل البيانات: " + ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void btnAdd_Click(object? sender, EventArgs e)
    {
        var editor = ActivatorUtilities.CreateInstance<ProductEditorForm>(_serviceProvider);
        editor.ShowDialog();
    }

    private void btnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvProducts.CurrentRow?.DataBoundItem is not ProductDto product) return;
        var editor = ActivatorUtilities.CreateInstance<ProductEditorForm>(_serviceProvider, product.Id);
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

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(5) };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };

        searchBar = new SearchBarControl { Width = 300 };
        searchBar.SearchChanged += async (_, _) => await LoadDataAsync();

        chkShowInactive = new CheckBox { Text = "عرض المعطل", AutoSize = true, Margin = new Padding(0, 10, 10, 0) };
        chkShowInactive.CheckedChanged += async (_, _) => await LoadDataAsync();

        btnAdd = new Button { Text = "إضافة منتج", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += btnAdd_Click;

        btnEdit = new Button { Text = "تعديل", Width = 80, Height = 35, FlatStyle = FlatStyle.Flat };
        btnEdit.Click += btnEdit_Click;

        btnToggleActive = new Button { Text = "تنشيط/تعطيل", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat };
        btnToggleActive.Click += btnToggleActive_Click;

        btnRefresh = new Button { Text = "تحديث", Width = 80, Height = 35, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadDataAsync();

        toolbar.Controls.AddRange(new Control[] { btnRefresh, btnToggleActive, btnEdit, btnAdd, chkShowInactive, searchBar });
        topPanel.Controls.Add(toolbar);

        dgvProducts = new DataGridView { Dock = DockStyle.Fill };
        lblCount = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };

        this.Controls.Add(dgvProducts);
        this.Controls.Add(lblCount);
        this.Controls.Add(topPanel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}

