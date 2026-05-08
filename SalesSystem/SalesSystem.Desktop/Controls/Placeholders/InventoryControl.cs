using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class InventoryControl : BaseModuleControl
{
    private readonly IInventoryApiService _apiService;
    private readonly INotificationService _notification;
    
    private DataGridView _grid;
    private SearchBarControl _searchBar;
    private LoadingOverlayControl _loadingOverlay;
    private BindingList<InventoryMovementDto> _movementList = new();

    public InventoryControl(
        IInventoryApiService apiService,
        INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        
        InitializeComponent();
        SetupGrid();
    }

    private void InitializeComponent()
    {
        this.pnlTop = new System.Windows.Forms.Panel();
        this._searchBar = new SalesSystem.Desktop.Controls.Common.SearchBarControl();
        this._loadingOverlay = new SalesSystem.Desktop.Controls.Common.LoadingOverlayControl();
        this._grid = new System.Windows.Forms.DataGridView();
        
        this.pnlTop.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();

        this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlTop.Height = 60;
        this.pnlTop.Controls.Add(this._searchBar);
        this.pnlTop.Padding = new System.Windows.Forms.Padding(10);

        this._searchBar.Location = new System.Drawing.Point(10, 12);
        this._searchBar.Placeholder = "بحث في الحركات المخزنية...";
        this._searchBar.SearchChanged += async (s, text) => await RefreshData();

        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.BackgroundColor = System.Drawing.Color.White;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.AllowUserToAddRows = false;
        this._grid.ReadOnly = true;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

        this.Controls.Add(this._loadingOverlay);
        this.Controls.Add(this._grid);
        this.Controls.Add(this.pnlTop);

        this.pnlTop.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlTop;

    private void SetupGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MovementDate", HeaderText = "التاريخ", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "المنتج", FillWeight = 2 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WarehouseName", HeaderText = "المستودع", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MovementType", HeaderText = "نوع الحركة", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityChange", HeaderText = "التغير", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityAfter", HeaderText = "الرصيد بعدها", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReferenceType", HeaderText = "المرجع", Width = 100 });
        
        foreach (DataGridViewColumn col in _grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        
        _grid.CellFormatting += (s, e) => {
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == "MovementType" && e.Value != null)
            {
                e.Value = (byte)e.Value switch { 
                    1 => "شراء", 2 => "بيع", 3 => "مرتجع مبيعات", 
                    4 => "مرتجع مشتريات", 5 => "تحويل صادر", 6 => "تحويل وارد", 7 => "تعديل", _ => "أخرى" 
                };
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
        var result = await _apiService.GetMovementsAsync(_searchBar.SearchText);
        _loadingOverlay.HideOverlay();

        if (result.IsSuccess)
        {
            _movementList = new BindingList<InventoryMovementDto>(result.Value.ToList());
            _grid.DataSource = _movementList;
        }
        else _notification.ShowError(result.Error!);
    }

    protected override void RegisterSubscriptions() { }
}
