using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Warehouses;

public partial class WarehousesListControl : UserControl
{
    private readonly IWarehouseApiService _warehouseApi;
    private readonly IInventoryApiService _inventoryApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly ISessionService _session;
    private readonly BindingSource _warehouseBindingSource = new();
    private readonly BindingSource _stockBindingSource = new();
    private IDisposable? _subscription;

    private DataGridView dgvWarehouses = null!;
    private DataGridView dgvStock = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnRefresh = null!;
    private Label lblStatus = null!;

    public WarehousesListControl(
        IWarehouseApiService warehouseApi,
        IInventoryApiService inventoryApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification,
        ISessionService session)
    {
        _warehouseApi = warehouseApi;
        _inventoryApi = inventoryApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        _session = session;
        
        InitializeComponent();
        SetupGrids();
        ApplyPermissions();
    }

    private void SetupGrids()
    {
        this.RightToLeft = RightToLeft.Yes;

        // Warehouse Grid
        dgvWarehouses.DataSource = _warehouseBindingSource;
        dgvWarehouses.AutoGenerateColumns = false;
        dgvWarehouses.ReadOnly = true;
        dgvWarehouses.AllowUserToAddRows = false;
        dgvWarehouses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvWarehouses.MultiSelect = false;
        dgvWarehouses.BackgroundColor = Color.White;

        dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم المستودع", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Location", HeaderText = "الموقع", Width = 150 });
        dgvWarehouses.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsDefault", HeaderText = "افتراضي", Width = 70 });
        dgvWarehouses.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 70 });

        dgvWarehouses.SelectionChanged += DgvWarehouses_SelectionChanged;

        // Stock Grid
        dgvStock.DataSource = _stockBindingSource;
        dgvStock.AutoGenerateColumns = false;
        dgvStock.ReadOnly = true;
        dgvStock.AllowUserToAddRows = false;
        dgvStock.BackgroundColor = Color.WhiteSmoke;

        dgvStock.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductCode", HeaderText = "كود المنتج", Width = 100 });
        dgvStock.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "اسم المنتج", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvStock.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "الكمية", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
    }

    private void ApplyPermissions()
    {
        bool isAdmin = _session.Current?.Role == UserRole.Admin;
        btnAdd.Visible = isAdmin;
        btnEdit.Visible = isAdmin;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<WarehouseChangedMessage>(async _ => await LoadWarehousesAsync());
        await LoadWarehousesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await _warehouseApi.GetAllAsync();
            if (result.IsSuccess)
            {
                _warehouseBindingSource.DataSource = result.Value;
                lblStatus.Text = $"عدد المستودعات: {result.Value.Count}";
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            _notification.ShowError("خطأ في تحميل المستودعات: " + ex.Message);
        }
    }

    private async void DgvWarehouses_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvWarehouses.CurrentRow?.DataBoundItem is WarehouseDto w)
        {
            await LoadStockAsync(w.Id);
        }
        else
        {
            _stockBindingSource.DataSource = null;
        }
    }

    private async Task LoadStockAsync(int warehouseId)
    {
        try
        {
            var result = await _inventoryApi.GetStockByWarehouseAsync(warehouseId);
            if (result.IsSuccess)
            {
                _stockBindingSource.DataSource = result.Value;
            }
        }
        catch { /* Ignore */ }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        
        btnRefresh = new Button { Text = "تحديث", Width = 80, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadWarehousesAsync();

        btnAdd = new Button { Text = "مستودع جديد", Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Width = 80, FlatStyle = FlatStyle.Flat };
        btnEdit.Click += (_, _) => {
            if (dgvWarehouses.CurrentRow?.DataBoundItem is WarehouseDto w) ShowEditor(w);
        };

        topPanel.Controls.AddRange(new Control[] { btnEdit, btnAdd, btnRefresh });

        var splitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
        dgvWarehouses = new DataGridView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
        dgvStock = new DataGridView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
        
        var stockPanel = new Panel { Dock = DockStyle.Fill };
        var stockLabel = new Label { Text = "المخزون المتوفر في المستودع المختار", Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(236, 240, 241), TextAlign = ContentAlignment.MiddleCenter, Font = new Font(this.Font, FontStyle.Bold) };
        stockPanel.Controls.Add(dgvStock);
        stockPanel.Controls.Add(stockLabel);

        splitContainer.Panel1.Controls.Add(dgvWarehouses);
        splitContainer.Panel2.Controls.Add(stockPanel);

        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(splitContainer, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);

        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(WarehouseDto? w = null)
    {
        var editor = ActivatorUtilities.CreateInstance<WarehouseEditorForm>(_serviceProvider, w ?? (object)Type.Missing);
        editor.ShowDialog();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
