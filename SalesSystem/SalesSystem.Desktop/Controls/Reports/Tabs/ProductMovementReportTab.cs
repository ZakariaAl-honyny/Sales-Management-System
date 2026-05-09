using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class ProductMovementReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgv;
    public string GetReportName() => "ط­ط±ظƒط© ظ…ظ†طھط¬";

    private readonly IReportApiService _reportApi;
    private readonly IProductApiService _productApi;
    private readonly INotificationService _notification;

    private ComboBox cmbProduct = null!;
    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private DataGridView dgv = null!;
    private Label lblTotal = null!;
    private Panel? _loadingOverlay;

    public ProductMovementReportTab(
        IReportApiService reportApi,
        IProductApiService productApi,
        INotificationService notification)
    {
        _reportApi = reportApi;
        _productApi = productApi;
        _notification = notification;

        InitializeComponent();
        Load += async (_, _) => await LoadProductsAsync();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        RightToLeft = RightToLeft.Yes;

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
        var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };

        flp.Controls.Add(new Label { Text = "ط§ظ„ظ…ظ†طھط¬:", Width = 60 });
        cmbProduct = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        flp.Controls.Add(cmbProduct);

        flp.Controls.Add(new Label { Text = "ظ…ظ†:", Width = 40 });
        dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        dtpFrom.Value = DateTime.Now.AddMonths(-1);
        flp.Controls.Add(dtpFrom);

        flp.Controls.Add(new Label { Text = "ط¥ظ„ظ‰:", Width = 40 });
        dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        dtpTo.Value = DateTime.Now;
        flp.Controls.Add(dtpTo);

        var btnLoad = new Button { Text = "ط¹ط±ط¶", Width = 80 };
        btnLoad.Click += async (_, _) => await LoadReportAsync();
        flp.Controls.Add(btnLoad);

        pnlTop.Controls.Add(flp);

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };

        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "ط§ظ„طھط§ط±ظٹط®", DataPropertyName = "MovementDate", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "ط§ظ„ظ†ظˆط¹", DataPropertyName = "MovementType", Width = 100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Warehouse", HeaderText = "ط§ظ„ظ…ط®ط²ظ†", DataPropertyName = "WarehouseName", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyChange", HeaderText = "ط§ظ„ظƒظ…ظٹط©", DataPropertyName = "QuantityChange", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Before", HeaderText = "ظ‚ط¨ظ„", DataPropertyName = "QuantityBefore", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "After", HeaderText = "ط¨ط¹ط¯", DataPropertyName = "QuantityAfter", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "ط§ظ„ظ…ط±ط¬ط¹", DataPropertyName = "ReferenceType", Width = 100 });

        lblTotal = new Label 
        { 
            Dock = DockStyle.Bottom, 
            Height = 30, 
            TextAlign = ContentAlignment.MiddleLeft, 
            ForeColor = Color.Green, 
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Padding = new Padding(10, 0, 0, 0)
        };

        var pnlMain = new Panel { Dock = DockStyle.Fill };
        pnlMain.Controls.Add(dgv);

        Controls.Add(pnlMain);
        Controls.Add(pnlTop);
        Controls.Add(lblTotal);
    }

    private void ShowLoading(bool show)
    {
        if (show)
        {
            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(128, 255, 255, 255)
            };
            var lbl = new Label
            {
                Text = "ط¬ط§ط±ظٹ ط§ظ„طھط­ظ…ظٹظ„...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14),
                Dock = DockStyle.Fill
            };
            _loadingOverlay.Controls.Add(lbl);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();
        }
        else
        {
            _loadingOverlay?.Dispose();
            _loadingOverlay = null;
        }
    }

    private async Task LoadProductsAsync()
    {
        var result = await _productApi.GetAllAsync();
        if (result.IsSuccess)
        {
            var products = result.Value.ToList();
            products.Insert(0, new ProductDto(0, "", "", "-- اختر منتج --", null, null, null, null, 0, 0, 0, "", true));
            cmbProduct.DataSource = products;
            cmbProduct.DisplayMember = "Name";
            cmbProduct.ValueMember = "Id";
        }
    }

    private async Task LoadReportAsync()
    {
        if (cmbProduct.SelectedValue is not int productId || productId == 0)
        {
            _notification.ShowWarning("ظٹط±ط¬ظ‰ ط§ط®طھظٹط§ط± ظ…ظ†طھط¬");
            return;
        }

        ShowLoading(true);
        try
        {
            var result = await _reportApi.GetMovementsAsync(productId, null, dtpFrom.Value, dtpTo.Value);
            if (result.IsSuccess)
            {
                var data = result.Value.ToList();
                dgv.DataSource = data;
                lblTotal.Text = $"ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ط­ط±ظƒط§طھ: {data.Count}";
            }
            else
            {
                _notification.ShowError(result.Error ?? "ط­ط¯ط« ط®ط·ط£");
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError(ex.Message);
        }
        finally
        {
            ShowLoading(false);
        }
    }
}







