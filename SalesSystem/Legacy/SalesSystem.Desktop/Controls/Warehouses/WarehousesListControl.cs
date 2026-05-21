using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Warehouses;

[System.ComponentModel.DesignerCategory("Code")]
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
        // Warehouse Grid
        dgvWarehouses.DataSource = _warehouseBindingSource;
        dgvWarehouses.AutoGenerateColumns = false;
        dgvWarehouses.ReadOnly = true;
        dgvWarehouses.AllowUserToAddRows = false;
        dgvWarehouses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvWarehouses.MultiSelect = false;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvWarehouses);

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
        
        ThemeHelper.ApplyDataGridViewStyle(dgvStock);
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
        this.RightToLeft = RightToLeft.Yes;
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
                lblStatus.Text = $"إجمالي عدد المستودعات: {result.Value!.Count}";
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل المستودعات");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات. تم تسجيل التفاصيل للدعم الفني.");
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
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل مخزون المستودع {WarehouseId}", warehouseId);
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var toolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        btnAdd = new Button { Text = "مستودع جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        btnEdit.Click += (_, _) => {
            if (dgvWarehouses.CurrentRow?.DataBoundItem is WarehouseDto w) ShowEditor(w);
        };

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await LoadWarehousesAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnRefresh });
        topPanel.Controls.Add(toolbar);

        var splitContainer = new SplitContainer { 
            Dock = DockStyle.Fill, 
            Orientation = Orientation.Horizontal, 
            SplitterDistance = 300,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(224, 224, 224)
        };
        
        dgvWarehouses = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvWarehouses);
        
        dgvStock = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvStock);
        
        var stockPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var stockLabel = new Label { 
            Text = "تفاصيل المخزون في المستودع المختار", 
            Dock = DockStyle.Top, 
            Height = 35, 
            BackColor = Color.FromArgb(248, 249, 250), 
            TextAlign = ContentAlignment.MiddleCenter, 
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary
        };
        stockPanel.Controls.Add(dgvStock);
        stockPanel.Controls.Add(stockLabel);

        splitContainer.Panel1.Controls.Add(dgvWarehouses);
        splitContainer.Panel1.BackColor = Color.White;
        splitContainer.Panel2.Controls.Add(stockPanel);
        splitContainer.Panel2.BackColor = Color.White;

        lblStatus = new Label { 
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
        mainLayout.Controls.Add(splitContainer, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);

        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(WarehouseDto? w = null)
    {
        var editor = _serviceProvider.GetRequiredService<WarehouseEditorForm>();
        editor.LoadData(w);
        editor.ShowDialog();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
