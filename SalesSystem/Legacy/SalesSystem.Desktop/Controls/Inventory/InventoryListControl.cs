using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Helpers;
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
        SetupGrid();
    }

    private void SetupGrid()
    {
        dgvMovements.DataSource = _bindingSource;
        dgvMovements.ReadOnly = true;
        dgvMovements.AllowUserToAddRows = false;
        dgvMovements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvMovements);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.RightToLeft = RightToLeft.Yes;
        await LoadMovementsAsync();
    }

    private async Task LoadMovementsAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _apiService.GetMovementsAsync(null, null, null, null); 
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatusLabel.Text = $"إجمالي الحركات: {result.Value!.Count}";
                FormatGrid();
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل حركات المخزون");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally { SetBusy(false); }
    }

    private void FormatGrid()
    {
        if (dgvMovements.Columns.Count == 0) return;
        
        SetHeader("MovementDate", "التاريخ والوقت");
        SetHeader("ProductName", "اسم المنتج");
        SetHeader("WarehouseName", "المستودع");
        SetHeader("MovementType", "نوع الحركة");
        SetHeader("QuantityChange", "الكمية");
        SetHeader("QuantityBefore", "قبل");
        SetHeader("QuantityAfter", "بعد");
        SetHeader("ReferenceType", "المرجع");
        SetHeader("ReferenceId", "رقم المرجع");

        if (dgvMovements.Columns.Contains("MovementDate"))
            dgvMovements.Columns["MovementDate"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";

        dgvMovements.CellFormatting += (s, e) => {
            if (e.RowIndex >= 0 && dgvMovements.Columns[e.ColumnIndex].Name == "MovementType" && e.Value != null)
            {
                e.Value = (MovementType)e.Value switch { 
                    MovementType.PurchaseIn => "مشتريات",
                    MovementType.SaleOut => "مبيعات",
                    MovementType.SaleReturnIn => "مردود مبيعات",
                    MovementType.PurchaseReturnOut => "مردود مشتريات",
                    MovementType.TransferOut => "تحويل صادر",
                    MovementType.TransferIn => "تحويل وارد",
                    MovementType.Adjustment => "تسوية",
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
        foreach (Control c in this.Controls) c.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
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

        btnSearch = new Button { Text = "بحث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnSearch, ThemeHelper.ButtonType.Secondary);
        btnSearch.Click += async (_, _) => await LoadMovementsAsync();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadMovementsAsync(); };

        txtSearch = new TextBox { Width = 300, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "بحث باسم المنتج أو المستودع...";

        toolbar.Controls.AddRange(new Control[] { btnRefresh, btnSearch, txtSearch });
        topPanel.Controls.Add(toolbar);

        dgvMovements = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        ThemeHelper.ApplyDataGridViewStyle(dgvMovements);
        
        lblStatusLabel = new Label { 
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
        mainLayout.Controls.Add(dgvMovements, 0, 1);
        mainLayout.Controls.Add(lblStatusLabel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}
