using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Returns;

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
        var res = await _salesReturnApi.GetAllAsync();
        if (res.IsSuccess) _salesSource.DataSource = res.Value.Select(x => new { x.Id, x.ReturnNo, x.ReturnDate, CustomerName = x.CustomerName ?? "نقدي", x.TotalAmount, StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "؟" }, Original = x }).ToList();
    }

    private async Task LoadPurchasesAsync()
    {
        var res = await _purchaseReturnApi.GetAllAsync();
        if (res.IsSuccess) _purchaseSource.DataSource = res.Value.Select(x => new { x.Id, x.ReturnNo, x.ReturnDate, x.SupplierName, x.TotalAmount, StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "؟" }, Original = x }).ToList();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        var btnAddSales = new Button { Text = "مرتجع مبيعات جديد", Width = 140, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White };
        btnAddSales.Click += (s, e) => ShowSalesEditor();
        var btnAddPurchase = new Button { Text = "مرتجع مشتريات جديد", Width = 140, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White };
        btnAddPurchase.Click += (s, e) => ShowPurchaseEditor();
        pnlTop.Controls.AddRange(new Control[] { btnAddPurchase, btnAddSales });

        tabReturns = new TabControl { Dock = DockStyle.Fill };
        tpSales = new TabPage("مرتجعـات المبيعات");
        tpPurchases = new TabPage("مرتجعـات المشتريات");
        dgvSales = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };
        dgvPurchases = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };
        
        tpSales.Controls.Add(dgvSales);
        tpPurchases.Controls.Add(dgvPurchases);
        tabReturns.TabPages.AddRange(new TabPage[] { tpSales, tpPurchases });

        mainLayout.Controls.Add(pnlTop, 0, 0);
        mainLayout.Controls.Add(tabReturns, 0, 1);
        this.Controls.Add(mainLayout);
    }

    private void ShowSalesEditor(SalesReturnDto? p = null) {
        var form = ActivatorUtilities.CreateInstance<SalesReturnForm>(_serviceProvider, p ?? (object)Type.Missing);
        form.ShowDialog();
    }

    private void ShowPurchaseEditor(PurchaseReturnDto? p = null) {
        var form = ActivatorUtilities.CreateInstance<PurchaseReturnForm>(_serviceProvider, p ?? (object)Type.Missing);
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing) { if (disposing) { _sub1?.Dispose(); _sub2?.Dispose(); } base.Dispose(disposing); }
}
