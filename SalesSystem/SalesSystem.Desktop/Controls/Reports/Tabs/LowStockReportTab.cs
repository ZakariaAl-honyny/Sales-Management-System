using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using ClosedXML.Excel;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class LowStockReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgvReport;
    public string GetReportName() => "طھظ†ط¨ظٹظ‡ ط§ظ„ظ…ط®ط²ظˆظ† ط§ظ„ظ…ظ†ط®ظپط¶";

    private readonly IReportApiService _reportApi;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();

    private Button btnRefresh = null!;
    private Button btnExport = null!;
    private DataGridView dgvReport = null!;
    private Label lblSummary = null!;
    private Panel loadingPanel = null!;

    public LowStockReportTab(IReportApiService reportApi, INotificationService notification)
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
        dgvReport.CellFormatting += DgvReport_CellFormatting;
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
            var result = await _reportApi.GetLowStockAsync();
            
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

    private void UpdateSummary(List<LowStockReportDto> list)
    {
        if (list.Count == 0)
        {
            lblSummary.Text = "ط¬ظ…ظٹط¹ ط§ظ„ط£طµظ†ط§ظپ ظپظˆظ‚ ط§ظ„ط­ط¯ ط§ظ„ط£ط¯ظ†ظ‰ ظ„ظ„ظ…ط®ط²ظˆظ†";
            return;
        }

        var totalQty = list.Sum(x => x.Quantity);
        lblSummary.Text = $"ط¹ط¯ط¯ ط§ظ„ط£طµظ†ط§ظپ ط£ظ‚ظ„ ظ…ظ† ط§ظ„ط­ط¯ ط§ظ„ط£ط¯ظ†ظ‰: {list.Count} | ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظƒظ…ظٹط©: {totalQty:N3}";
    }

    private void FormatGrid()
    {
        if (dgvReport.Columns.Count == 0) return;

        var hides = new[] { "ProductId", "WarehouseId" };
        foreach (var h in hides)
        {
            if (dgvReport.Columns.Contains(h)) dgvReport.Columns[h].Visible = false;
        }

        SetHeader("ProductCode", "ظƒظˆط¯ ط§ظ„ظ…ظ†طھط¬");
        SetHeader("ProductName", "ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬");
        SetHeader("UnitName", "ط§ظ„ظˆط­ط¯ط©");
        SetHeader("WarehouseName", "ط§ظ„ظ…ط³طھظˆط¯ط¹");
        SetHeader("Quantity", "ط§ظ„ظƒظ…ظٹط© ط§ظ„ط­ط§ظ„ظٹط©");
        SetHeader("ReorderLevel", "ط§ظ„ط­ط¯ ط§ظ„ط£ط¯ظ†ظ‰");

        if (dgvReport.Columns.Contains("Quantity"))
            dgvReport.Columns["Quantity"].DefaultCellStyle.Format = "N3";
        
        if (dgvReport.Columns.Contains("ReorderLevel"))
            dgvReport.Columns["ReorderLevel"].DefaultCellStyle.Format = "N3";
    }

    private void DgvReport_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvReport.Columns[e.ColumnIndex].Name == "Quantity")
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 200, 200);
            e.CellStyle.ForeColor = Color.Red;
            e.CellStyle.Font = new Font(dgvReport.Font, FontStyle.Bold);
        }
    }

    private void SetHeader(string col, string text)
    {
        if (dgvReport.Columns.Contains(col)) dgvReport.Columns[col].HeaderText = text;
    }

    private async Task ExportToExcelAsync()
    {
        if (_bindingSource.DataSource is not List<LowStockReportDto> data || data.Count == 0)
        {
            _notification.ShowWarning("ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ظ„ظ„طھطµط¯ظٹط±");
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("طھظ†ط¨ظٹظ‡ ط§ظ„ظ…ط®ط²ظˆظ† ط§ظ„ظ…ظ†ط®ظپط¶");

            var headers = new[] { "ظƒظˆط¯ ط§ظ„ظ…ظ†طھط¬", "ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬", "ط§ظ„ظˆط­ط¯ط©", "ط§ظ„ظ…ط³طھظˆط¯ط¹", "ط§ظ„ظƒظ…ظٹط© ط§ظ„ط­ط§ظ„ظٹط©", "ط§ظ„ط­ط¯ ط§ظ„ط£ط¯ظ†ظ‰" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cell(row + 2, 1).Value = item.ProductCode ?? "-";
                worksheet.Cell(row + 2, 2).Value = item.ProductName;
                worksheet.Cell(row + 2, 3).Value = item.UnitName ?? "-";
                worksheet.Cell(row + 2, 4).Value = item.WarehouseName;
                worksheet.Cell(row + 2, 5).Value = item.Quantity;
                worksheet.Cell(row + 2, 6).Value = item.ReorderLevel;

                var range = worksheet.Range(row + 2, 1, row + 2, 6);
                range.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 200, 200);
                range.Style.Font.FontColor = XLColor.Red;
            }

            worksheet.Columns().AdjustToContents();

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"طھظ†ط¨ظٹظ‡_ط§ظ„ظ…ط®ط²ظˆظ†_ط§ظ„ظ…ظ†ط®ظپط¶_{DateTime.Now:yyyyMMdd}.xlsx"
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
        
        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadReportAsync();

        btnExport = new Button { Text = "طھطµط¯ظٹط± Excel", Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnExport.Click += async (_, _) => await ExportToExcelAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnExport, btnRefresh });
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




