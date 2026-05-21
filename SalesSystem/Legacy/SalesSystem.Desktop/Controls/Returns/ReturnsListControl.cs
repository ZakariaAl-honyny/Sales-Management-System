using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Returns;

[System.ComponentModel.DesignerCategory("Code")]
public partial class ReturnsListControl : UserControl
{
    private readonly ISalesReturnApiService _salesReturnApi;
    private readonly IPurchaseReturnApiService _purchaseReturnApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly ISessionService _session;
    
    private TabControl tabReturns = null!;
    private TabPage tpSales = null!;
    private TabPage tpPurchases = null!;
    private DataGridView dgvSales = null!;
    private DataGridView dgvPurchases = null!;
    private BindingSource _salesSource = new();
    private BindingSource _purchaseSource = new();
    private IDisposable? _sub1, _sub2;

    public ReturnsListControl(
        ISalesReturnApiService salesReturnApi,
        IPurchaseReturnApiService purchaseReturnApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification,
        ISessionService session)
    {
        _salesReturnApi = salesReturnApi;
        _purchaseReturnApi = purchaseReturnApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        _session = session;

        InitializeComponent();
        SetupGrids();
    }

    private void SetupGrids()
    {
        this.RightToLeft = RightToLeft.Yes;
        
        dgvSales.DataSource = _salesSource;
        dgvSales.AutoGenerateColumns = false;
        dgvSales.ReadOnly = true;
        dgvSales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnNo", HeaderText = "رقم المرتجع", Width = 120 });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", Width = 80, DataPropertyName = "StatusText" });
        dgvSales.CellFormatting += (s, e) => { if (dgvSales.Columns[e.ColumnIndex].Name == "Status" && e.Value != null) e.CellStyle.ForeColor = e.Value.ToString() == "مرحل" ? Color.Green : (e.Value.ToString() == "ملغي" ? Color.Red : Color.Blue); };
        dgvSales.DoubleClick += (s, e) => { if (dgvSales.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowSalesEditor(d.Original); } };

        dgvPurchases.DataSource = _purchaseSource;
        dgvPurchases.AutoGenerateColumns = false;
        dgvPurchases.ReadOnly = true;
        dgvPurchases.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnNo", HeaderText = "رقم المرتجع", Width = 120 });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", Width = 80, DataPropertyName = "StatusText" });
        dgvPurchases.CellFormatting += (s, e) => { if (dgvPurchases.Columns[e.ColumnIndex].Name == "Status" && e.Value != null) e.CellStyle.ForeColor = e.Value.ToString() == "مرحل" ? Color.Green : (e.Value.ToString() == "ملغي" ? Color.Red : Color.Blue); };
        dgvPurchases.DoubleClick += (s, e) => { if (dgvPurchases.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowPurchaseEditor(d.Original); } };
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _sub1 = _eventBus.Subscribe<SalesReturnChangedMessage>(async _ => await LoadSalesAsync());
        _sub2 = _eventBus.Subscribe<PurchaseReturnChangedMessage>(async _ => await LoadPurchasesAsync());
        await Task.WhenAll(LoadSalesAsync(), LoadPurchasesAsync());
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            var res = await _salesReturnApi.GetAllAsync();
            if (res.IsSuccess) _salesSource.DataSource = res.Value.Select(x => new { x.Id, x.ReturnNo, x.ReturnDate, CustomerName = x.CustomerName ?? "عميل نقدي", x.TotalAmount, StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "?" }, Original = x }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطأ في تحميل مرتجعات المبيعات");
            _notification.ShowError("حدث خطأ غير متوقع. تم تسجيل التفاصيل للدعم الفني.");
        }
    }

    private async Task LoadPurchasesAsync()
    {
        try
        {
            var res = await _purchaseReturnApi.GetAllAsync();
            if (res.IsSuccess) _purchaseSource.DataSource = res.Value.Select(x => new { x.Id, x.ReturnNo, x.ReturnDate, x.SupplierName, x.TotalAmount, StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "?" }, Original = x }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطأ في تحميل مرتجعات المشتريات");
            _notification.ShowError("حدث خطأ غير متوقع. تم تسجيل التفاصيل للدعم الفني.");
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var toolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var btnAddSales = new Button { Text = "مرتجع مبيعات جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAddSales, ThemeHelper.ButtonType.Primary);
        btnAddSales.Click += (s, e) => ShowSalesEditor();

        var btnAddPurchase = new Button { Text = "مرتجع مشتريات جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAddPurchase, ThemeHelper.ButtonType.Secondary);
        btnAddPurchase.Click += (s, e) => ShowPurchaseEditor();

        var btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await Task.WhenAll(LoadSalesAsync(), LoadPurchasesAsync());

        toolbar.Controls.AddRange(new Control[] { btnAddSales, btnAddPurchase, btnRefresh });
        topPanel.Controls.Add(toolbar);

        tabReturns = new TabControl { 
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F)
        };
        tpSales = new TabPage("مرتجعـات المبيعات") { BackColor = Color.White };
        tpPurchases = new TabPage("مرتجعـات المشتريات") { BackColor = Color.White };
        
        dgvSales = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvSales);
        
        dgvPurchases = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvPurchases);
        
        tpSales.Controls.Add(dgvSales);
        tpPurchases.Controls.Add(dgvPurchases);
        tabReturns.TabPages.AddRange(new TabPage[] { tpSales, tpPurchases });

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(tabReturns, 0, 1);
        this.Controls.Add(mainLayout);
    }

    private void ShowSalesEditor(SalesReturnDto? p = null) {
        var form = _serviceProvider.GetRequiredService<SalesReturnForm>();
        form.LoadData(p);
        form.ShowDialog();
    }

    private void ShowPurchaseEditor(PurchaseReturnDto? p = null) {
        var form = _serviceProvider.GetRequiredService<PurchaseReturnForm>();
        form.LoadData(p);
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing) { if (disposing) { _sub1?.Dispose(); _sub2?.Dispose(); } base.Dispose(disposing); }
}
