using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class CustomerPaymentsControl : BaseModuleControl
{
    private readonly ICustomerPaymentApiService _apiService;
    private readonly ICustomerApiService _customerApi;
    private readonly INotificationService _notification;
    
    private DataGridView _grid;
    private SearchBarControl _searchBar;
    private LoadingOverlayControl _loadingOverlay;
    private BindingList<CustomerPaymentDto> _paymentList = new();

    public CustomerPaymentsControl(
        ICustomerPaymentApiService apiService,
        ICustomerApiService customerApi,
        INotificationService notification)
    {
        _apiService = apiService;
        _customerApi = customerApi;
        _notification = notification;
        
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

        this.btnNew.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
        this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnNew.ForeColor = System.Drawing.Color.White;
        this.btnNew.Location = new System.Drawing.Point(10, 12);
        this.btnNew.Size = new System.Drawing.Size(120, 35);
        this.btnNew.Text = "+ سند قبض";
        this.btnNew.Click += (s, e) => ShowAddDialog();

        this._searchBar.Location = new System.Drawing.Point(140, 12);
        this._searchBar.Placeholder = "بحث في السندات...";
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
    private System.Windows.Forms.Button btnNew;

    private void SetupGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VoucherNo", HeaderText = "رقم السند", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", FillWeight = 2 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "المبلغ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "التاريخ", Width = 120 });
        
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
        var result = await _apiService.GetAllAsync(_searchBar.SearchText);
        _loadingOverlay.HideOverlay();

        if (result.IsSuccess)
        {
            _paymentList = new BindingList<CustomerPaymentDto>(result.Value.ToList());
            _grid.DataSource = _paymentList;
        }
        else _notification.ShowError(result.Error!);
    }

    private void ShowAddDialog()
    {
        using var diag = new CustomerPaymentDialog(_apiService, _customerApi, _notification);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    protected override void RegisterSubscriptions() { }
}
