using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.Desktop.Controls.Inventory;

public partial class InventoryListControl : UserControl
{
    private readonly IReportApiService _apiService;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    
    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnRefresh = null!;
    private DataGridView dgvMovements = null!;
    private Label lblStatusLabel = null!;

    public InventoryListControl(
        IReportApiService apiService,
        INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        dgvMovements.DataSource = _bindingSource;
        dgvMovements.ReadOnly = true;
        dgvMovements.AllowUserToAddRows = false;
        dgvMovements.BackgroundColor = Color.White;
        dgvMovements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadMovementsAsync();
    }

    private async Task LoadMovementsAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _apiService.GetMovementsAsync(null, null, null, null); // Or pass search filter if API supports it
            
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatusLabel.Text = $"ط¹ط¯ط¯ ط§ظ„ط­ط±ظƒط§طھ: {result.Value.Count}";
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FormatGrid()
    {
        if (dgvMovements.Columns.Count == 0) return;
        
        SetHeader("MovementDate", "ط§ظ„طھط§ط±ظٹط®");
        SetHeader("ProductName", "ط§ظ„ظ…ظ†طھط¬");
        SetHeader("WarehouseName", "ط§ظ„ظ…ط³طھظˆط¯ط¹");
        SetHeader("MovementType", "ظ†ظˆط¹ ط§ظ„ط­ط±ظƒط©");
        SetHeader("QuantityChange", "ط§ظ„طھط؛ظٹط±");
        SetHeader("QuantityBefore", "ظ‚ط¨ظ„");
        SetHeader("QuantityAfter", "ط¨ط¹ط¯");
        SetHeader("ReferenceType", "ط§ظ„ظ…ط±ط¬ط¹");
        SetHeader("ReferenceId", "ط±ظ‚ظ… ط§ظ„ظ…ط±ط¬ط¹");

        if (dgvMovements.Columns.Contains("MovementDate"))
            dgvMovements.Columns["MovementDate"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";

        dgvMovements.CellFormatting += (s, e) => {
            if (e.RowIndex >= 0 && dgvMovements.Columns[e.ColumnIndex].Name == "MovementType" && e.Value != null)
            {
                e.Value = (MovementType)e.Value switch { 
                    MovementType.PurchaseIn => "ط´ط±ط§ط،",
                    MovementType.SaleOut => "ط¨ظٹط¹",
                    MovementType.SaleReturnIn => "ظ…ط±طھط¬ط¹ ظ…ط¨ظٹط¹ط§طھ",
                    MovementType.PurchaseReturnOut => "ظ…ط±طھط¬ط¹ ظ…ط´طھط±ظٹط§طھ",
                    MovementType.TransferOut => "طھط­ظˆظٹظ„ طµط§ط¯ط±",
                    MovementType.TransferIn => "طھط­ظˆظٹظ„ ظˆط§ط±ط¯",
                    MovementType.Adjustment => "طھط¹ط¯ظٹظ„",
                    _ => e.Value
                };
                e.FormattingApplied = true;
            }
        };
    }

    private void SetHeader(string col, string text)
    {
        if (dgvMovements.Columns.Contains(col)) dgvMovements.Columns[col].HeaderText = text;
    }

    private void SetBusy(bool busy)
    {
        txtSearch.Enabled = !busy;
        btnSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        dgvMovements.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        txtSearch = new TextBox { Width = 250, PlaceholderText = "ط¨ط­ط« ظپظٹ ط§ظ„ط­ط±ظƒط§طھ ط§ظ„ظ…ط®ط²ظ†ظٹط©..." };
        btnSearch = new Button { Text = "ط¨ط­ط«", Width = 80, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadMovementsAsync();

        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 80, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadMovementsAsync(); };

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnRefresh, btnSearch, txtSearch });
        topPanel.Controls.Add(flow);

        dgvMovements = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        lblStatusLabel = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Text = "ط¬ط§ظ‡ط²" };

        this.Controls.Add(dgvMovements);
        this.Controls.Add(lblStatusLabel);
        this.Controls.Add(topPanel);
    }
}



