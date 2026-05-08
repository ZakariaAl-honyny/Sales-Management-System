using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class PurchasesControl : BaseModuleControl
{
    private readonly IPurchaseInvoiceApiService _apiService;
    private readonly ISupplierApiService _supplierApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    private readonly IServiceProvider _serviceProvider;
    
    private DataGridView _grid;
    private SearchBarControl _searchBar;
    private LoadingOverlayControl _loadingOverlay;
    private BindingList<PurchaseInvoiceDto> _invoiceList = new();

    public PurchasesControl(
        IPurchaseInvoiceApiService apiService,
        ISupplierApiService supplierApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification,
        IServiceProvider serviceProvider)
    {
        _apiService = apiService;
        _supplierApi = supplierApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;
        _serviceProvider = serviceProvider;
        
        InitializeComponent();
        SetupGrid();
    }

    private void InitializeComponent()
    {
        this.pnlTop = new System.Windows.Forms.Panel();
        this.btnNew = new System.Windows.Forms.Button();
        this._searchBar = new SalesSystem.Desktop.Controls.Common.SearchBarControl();
        this._loadingOverlay = new SalesSystem.Desktop.Controls.Common.LoadingOverlayControl();
        this._grid = new System.Windows.Forms.DataGridView();
        
        this.pnlTop.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();

        this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlTop.Height = 60;
        this.pnlTop.Controls.Add(this.btnNew);
        this.pnlTop.Controls.Add(this._searchBar);
        this.pnlTop.Padding = new System.Windows.Forms.Padding(10);

        this.btnNew.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnNew.ForeColor = System.Drawing.Color.White;
        this.btnNew.Location = new System.Drawing.Point(10, 12);
        this.btnNew.Size = new System.Drawing.Size(120, 35);
        this.btnNew.Text = "+ فاتورة مشتريات";
        this.btnNew.Click += (s, e) => ShowAddDialog();

        this._searchBar.Location = new System.Drawing.Point(140, 12);
        this._searchBar.Placeholder = "بحث عن فاتورة...";
        this._searchBar.SearchChanged += async (s, text) => await RefreshData();

        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.BackgroundColor = System.Drawing.Color.White;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.AllowUserToAddRows = false;
        this._grid.ReadOnly = true;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ShowEditDialog(_invoiceList[e.RowIndex]); };

        this.Controls.Add(this._loadingOverlay);
        this.Controls.Add(this._grid);
        this.Controls.Add(this.pnlTop);

        this.pnlTop.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlTop;
    private System.Windows.Forms.Button btnNew;

    private void SetupGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", FillWeight = 2 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "الحالة", Width = 80 });
        
        foreach (DataGridViewColumn col in _grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        
        _grid.CellFormatting += (s, e) => {
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
            {
                e.Value = (byte)e.Value switch { 1 => "مسودة", 2 => "مرحلة", 3 => "ملغاة", _ => "غير معروف" };
                e.FormattingApplied = true;
            }
        };
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        _loadingOverlay.ShowOverlay();
        var result = await _apiService.GetAllAsync(_searchBar.SearchText);
        _loadingOverlay.HideOverlay();

        if (result.IsSuccess)
        {
            _invoiceList = new BindingList<PurchaseInvoiceDto>(result.Value.ToList());
            _grid.DataSource = _invoiceList;
        }
        else _notification.ShowError(result.Error!);
    }

    private void ShowAddDialog()
    {
        using var diag = new PurchaseInvoiceForm(_apiService, _supplierApi, _productApi, _warehouseApi, _notification);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    private void ShowEditDialog(PurchaseInvoiceDto invoice)
    {
        using var diag = new PurchaseInvoiceForm(_apiService, _supplierApi, _productApi, _warehouseApi, _notification, invoice);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    protected override void RegisterSubscriptions() { }
}
