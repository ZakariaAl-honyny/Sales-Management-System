using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using ClosedXML.Excel;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class PurchasesReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgvReport;
    public string GetReportName() => "طھظ‚ط±ظٹط± ط§ظ„ظ…ط´طھط±ظٹط§طھ";

    private readonly IReportApiService _reportApi;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();

    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private Button btnRefresh = null!;
    private Button btnExport = null!;
    private DataGridView dgvReport = null!;
    private Label lblSummary = null!;
    private Panel loadingPanel = null!;

    public PurchasesReportTab(IReportApiService reportApi, INotificationService notification)
    {
        _reportApi = reportApi;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        dgvReport.DataSource = _bindingSource;
        dgvReport.ReadOnly = true;
        dgvReport.AllowUserToAddRows = false;
        dgvReport.BackgroundColor = Color.White;
        dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvReport.AutoGenerateColumns = true;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            ShowLoading(true);
            var result = await _reportApi.GetPurchasesAsync(dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1));
            
            if (result.IsSuccess)
            {
                var list = result.Value!.ToList();
                _bindingSource.DataSource = list;
                UpdateSummary(list);
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، طھط­ظ…ظٹظ„ ط§ظ„طھظ‚ط±ظٹط±: " + ex.Message);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void UpdateSummary(List<PurchaseReportDto> list)
    {
        if (list.Count == 0)
        {
            lblSummary.Text = "ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ";
            return;
        }

        var totalPurchases = list.Sum(x => x.TotalAmount);
        var totalPaid = list.Sum(x => x.PaidAmount);
        var totalDue = list.Sum(x => x.DueAmount);

        lblSummary.Text = $"ط¹ط¯ط¯ ط§ظ„ظپظˆط§طھظٹط±: {list.Count} | ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط´طھط±ظٹط§طھ: {totalPurchases:N2} | ط§ظ„ظ…ط¯ظپظˆط¹: {totalPaid:N2} | ط§ظ„ظ…طھط¨ظ‚ظٹ: {totalDue:N2}";
    }

    private void FormatGrid()
    {
        if (dgvReport.Columns.Count == 0) return;

        var hides = new[] { "InvoiceId", "WarehouseName", "SubTotal", "DiscountAmount", "TaxAmount" };
        foreach (var h in hides)
        {
            if (dgvReport.Columns.Contains(h)) dgvReport.Columns[h].Visible = false;
        }

        SetHeader("InvoiceNo", "ط±ظ‚ظ… ط§ظ„ظپط§طھظˆط±ط©");
        SetHeader("InvoiceDate", "ط§ظ„طھط§ط±ظٹط®");
        SetHeader("SupplierName", "ط§ظ„ظ…ظˆط±ط¯");
        SetHeader("TotalAmount", "ط§ظ„ط¥ط¬ظ…ط§ظ„ظٹ");
        SetHeader("PaidAmount", "ط§ظ„ظ…ط¯ظپظˆط¹");
        SetHeader("DueAmount", "ط§ظ„ظ…طھط¨ظ‚ظٹ");

        foreach (var col in new[] { "TotalAmount", "PaidAmount", "DueAmount" })
        {
            if (dgvReport.Columns.Contains(col))
            {
                dgvReport.Columns[col].DefaultCellStyle.Format = "N2";
                dgvReport.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }

        if (dgvReport.Columns.Contains("InvoiceDate"))
            dgvReport.Columns["InvoiceDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
    }

    private void SetHeader(string col, string text)
    {
        if (dgvReport.Columns.Contains(col)) dgvReport.Columns[col].HeaderText = text;
    }

    private async Task ExportToExcelAsync()
    {
        if (_bindingSource.DataSource is not List<PurchaseReportDto> data || data.Count == 0)
        {
            _notification.ShowWarning("ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ظ„ظ„طھطµط¯ظٹط±");
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("طھظ‚ط±ظٹط± ط§ظ„ظ…ط´طھط±ظٹط§طھ");

            // Headers
            var headers = new[] { "ط±ظ‚ظ… ط§ظ„ظپط§طھظˆط±ط©", "ط§ظ„طھط§ط±ظٹط®", "ط§ظ„ظ…ظˆط±ط¯", "ط§ظ„ط¥ط¬ظ…ط§ظ„ظٹ", "ط§ظ„ظ…ط¯ظپظˆط¹", "ط§ظ„ظ…طھط¨ظ‚ظٹ" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data
            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cell(row + 2, 1).Value = item.InvoiceNo;
                worksheet.Cell(row + 2, 2).Value = item.InvoiceDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row + 2, 3).Value = item.SupplierName;
                worksheet.Cell(row + 2, 4).Value = item.TotalAmount;
                worksheet.Cell(row + 2, 5).Value = item.PaidAmount;
                worksheet.Cell(row + 2, 6).Value = item.DueAmount;
            }

            worksheet.Columns().AdjustToContents();

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"طھظ‚ط±ظٹط±_ط§ظ„ظ…ط´طھط±ظٹط§طھ_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                workbook.SaveAs(dialog.FileName);
                _notification.ShowSuccess("طھظ… طھطµط¯ظٹط± ط§ظ„طھظ‚ط±ظٹط± ط¨ظ†ط¬ط§ط­");
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط§ظ„طھطµط¯ظٹط±: " + ex.Message);
        }
    }

    private void ShowLoading(bool show)
    {
        loadingPanel.Visible = show;
        this.Enabled = !show;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(10, 8, 10, 5) };
        
        var lblFrom = new Label { Text = "ظ…ظ†:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        dtpFrom.Value = DateTime.Today.AddMonths(-1);

        var lblTo = new Label { Text = "ط¥ظ„ظ‰:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        dtpTo.Value = DateTime.Today;

        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadReportAsync();

        btnExport = new Button { Text = "طھطµط¯ظٹط± Excel", Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnExport.Click += async (_, _) => await ExportToExcelAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnRefresh, dtpTo, lblTo, dtpFrom, lblFrom });
        filterPanel.Controls.Add(flow);

        dgvReport = new DataGridView { Dock = DockStyle.Fill };

        lblSummary = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "ط¬ط§ط±ظچ ط§ظ„طھط­ظ…ظٹظ„...",
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(5, 0, 10, 0)
        };

        loadingPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(200, Color.White),
            Visible = false
        };
        var loadingLabel = new Label
        {
            Text = "ط¬ط§ط±ظچ ط§ظ„طھط­ظ…ظٹظ„...",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243),
            AutoSize = true
        };
        loadingLabel.Location = new Point((Width - loadingLabel.Width) / 2, (Height - loadingLabel.Height) / 2);
        loadingPanel.Controls.Add(loadingLabel);

        this.Controls.Add(dgvReport);
        this.Controls.Add(lblSummary);
        this.Controls.Add(filterPanel);
        this.Controls.Add(loadingPanel);
    }
}




