using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class WarehousesControl : BaseModuleControl
{
    private readonly IWarehouseApiService _apiService;
    private readonly INotificationService _notificationService;
    
    private DataGridView _grid;
    private SearchBarControl _searchBar;
    private LoadingOverlayControl _loadingOverlay;
    private BindingList<WarehouseDto> _warehouseList = new();

    public WarehousesControl(
        IWarehouseApiService apiService,
        INotificationService notificationService)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        
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
        this.btnNew.Text = "+ إضافة جديد";
        this.btnNew.Click += (s, e) => ShowAddDialog();

        this._searchBar.Location = new System.Drawing.Point(140, 12);
        this._searchBar.Placeholder = "بحث عن مستودع...";
        this._searchBar.SearchChanged += (s, text) => FilterList(text);

        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.BackgroundColor = System.Drawing.Color.White;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.AllowUserToAddRows = false;
        this._grid.ReadOnly = true;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this._grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ShowEditDialog(_warehouseList[e.RowIndex]); };

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم المستودع", FillWeight = 1 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Location", HeaderText = "الموقع", FillWeight = 1 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsDefault", HeaderText = "افتراضي", Width = 80 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 60 });
        
        foreach (DataGridViewColumn col in _grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        _loadingOverlay.ShowOverlay();
        var result = await _apiService.GetAllAsync();
        _loadingOverlay.HideOverlay();

        if (result.IsSuccess)
        {
            _warehouseList = new BindingList<WarehouseDto>(result.Value.ToList());
            _grid.DataSource = _warehouseList;
        }
        else _notificationService.ShowError(result.Error!);
    }

    private void ShowAddDialog()
    {
        using var diag = new WarehouseDialog(_apiService, _notificationService);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    private void ShowEditDialog(WarehouseDto warehouse)
    {
        using var diag = new WarehouseDialog(_apiService, _notificationService, warehouse);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    private void FilterList(string text)
    {
        if (string.IsNullOrEmpty(text)) _grid.DataSource = _warehouseList;
        else
        {
            var filtered = _warehouseList.Where(w => w.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || (w.Code != null && w.Code.Contains(text, StringComparison.OrdinalIgnoreCase))).ToList();
            _grid.DataSource = new BindingList<WarehouseDto>(filtered);
        }
    }

    protected override void RegisterSubscriptions() { }
}
