using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using ClosedXML.Excel;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class CustomerBalanceReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgvReport;
    public string GetReportName() => "ظƒط´ظپ ط­ط³ط§ط¨ ط¹ظ…ظ„ط§ط،";

    private readonly IReportApiService _reportApi;
    private readonly ICustomerApiService? _customerApi;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();

    private ComboBox cmbCustomer = null!;
    private Button btnRefresh = null!;
    private Button btnExport = null!;
    private DataGridView dgvReport = null!;
    private Label lblSummary = null!;
    private Panel loadingPanel = null!;

    private IReadOnlyList<CustomerDto> _customers = new List<CustomerDto>();

    public CustomerBalanceReportTab(IReportApiService reportApi, INotificationService notification)
        : this(reportApi, null, notification)
    {
    }

    public CustomerBalanceReportTab(IReportApiService reportApi, ICustomerApiService? customerApi, INotificationService notification)
    {
        _reportApi = reportApi;
        _customerApi = customerApi;
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
        await LoadCustomersAsync();
        await LoadReportAsync();
    }

    private async Task LoadCustomersAsync()
    {
        if (_customerApi == null) return;

        try
        {
            var custResult = await _customerApi.GetAllAsync(null, true);
            if (custResult.IsSuccess)
            {
                _customers = custResult.Value;
                cmbCustomer.Items.Clear();
                cmbCustomer.Items.Add(new ComboBoxItem { Text = "ط¬ظ…ظٹط¹ ط§ظ„ط¹ظ…ظ„ط§ط،", Value = null });
                foreach (var cust in _customers)
                {
                    cmbCustomer.Items.Add(new ComboBoxItem { Text = cust.Name, Value = cust.Id });
                }
                cmbCustomer.SelectedIndex = 0;
            }
        }
        catch { }
    }

    private async Task LoadReportAsync()
    {
        try
        {
            ShowLoading(true);
            int? customerId = GetSelectedCustomerId();
            var result = await _reportApi.GetCustomerBalancesAsync(customerId);
            
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

    private int? GetSelectedCustomerId()
    {
        if (cmbCustomer.SelectedItem is ComboBoxItem item)
        {
            return item.Value as int?;
        }
        return null;
    }

    private void UpdateSummary(List<CustomerBalanceReportDto> list)
    {
        if (list.Count == 0)
        {
            lblSummary.Text = "ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ";
            return;
        }

        var totalBalance = list.Sum(x => x.CurrentBalance);
        var totalSales = list.Sum(x => x.TotalSales);
        var totalPayments = list.Sum(x => x.TotalPayments);

        lblSummary.Text = $"ط¹ط¯ط¯ ط§ظ„ط¹ظ…ظ„ط§ط،: {list.Count} | ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¨ظٹط¹ط§طھ: {totalSales:N2} | ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ: {totalPayments:N2} | ط§ظ„ط±طµظٹط¯: {totalBalance:N2}";
    }

    private void FormatGrid()
    {
        if (dgvReport.Columns.Count == 0) return;

        var hides = new[] { "CustomerId", "Phone" };
        foreach (var h in hides)
        {
            if (dgvReport.Columns.Contains(h)) dgvReport.Columns[h].Visible = false;
        }

        SetHeader("CustomerCode", "ظƒظˆط¯ ط§ظ„ط¹ظ…ظٹظ„");
        SetHeader("CustomerName", "ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„");
        SetHeader("OpeningBalance", "ط§ظ„ط±طµظٹط¯ ط§ظ„ط§ظپطھطھط§ط­ظٹ");
        SetHeader("TotalSales", "ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¨ظٹط¹ط§طھ");
        SetHeader("TotalPayments", "ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ");
        SetHeader("CurrentBalance", "ط§ظ„ط±طµظٹط¯ ط§ظ„ط­ط§ظ„ظٹ");

        foreach (var col in new[] { "OpeningBalance", "TotalSales", "TotalPayments", "CurrentBalance" })
        {
            if (dgvReport.Columns.Contains(col))
            {
                dgvReport.Columns[col].DefaultCellStyle.Format = "N2";
                dgvReport.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }
    }

    private void DgvReport_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvReport.Columns[e.ColumnIndex].Name == "CurrentBalance")
        {
            var row = dgvReport.Rows[e.RowIndex];
            if (row.DataBoundItem is CustomerBalanceReportDto item)
            {
                if (item.CurrentBalance > 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.Font = new Font(dgvReport.Font, FontStyle.Bold);
                }
                else if (item.CurrentBalance < 0)
                {
                    e.CellStyle.ForeColor = Color.Green;
                }
            }
        }
    }

    private void SetHeader(string col, string text)
    {
        if (dgvReport.Columns.Contains(col)) dgvReport.Columns[col].HeaderText = text;
    }

    private async Task ExportToExcelAsync()
    {
        if (_bindingSource.DataSource is not List<CustomerBalanceReportDto> data || data.Count == 0)
        {
            _notification.ShowWarning("ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ظ„ظ„طھطµط¯ظٹط±");
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ظƒط´ظپ ط­ط³ط§ط¨ ط¹ظ…ظ„ط§ط،");

            var headers = new[] { "ظƒظˆط¯ ط§ظ„ط¹ظ…ظٹظ„", "ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„", "ط§ظ„ط±طµظٹط¯ ط§ظ„ط§ظپطھطھط§ط­ظٹ", "ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¨ظٹط¹ط§طھ", "ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ", "ط§ظ„ط±طµظٹط¯ ط§ظ„ط­ط§ظ„ظٹ" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cell(row + 2, 1).Value = item.CustomerCode ?? "-";
                worksheet.Cell(row + 2, 2).Value = item.CustomerName;
                worksheet.Cell(row + 2, 3).Value = item.OpeningBalance;
                worksheet.Cell(row + 2, 4).Value = item.TotalSales;
                worksheet.Cell(row + 2, 5).Value = item.TotalPayments;
                worksheet.Cell(row + 2, 6).Value = item.CurrentBalance;

                if (item.CurrentBalance > 0)
                {
                    var cell = worksheet.Cell(row + 2, 6);
                    cell.Style.Font.FontColor = XLColor.Red;
                }
            }

            worksheet.Columns().AdjustToContents();

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ظƒط´ظپ_ط­ط³ط§ط¨_ط¹ظ…ظ„ط§ط،_{DateTime.Now:yyyyMMdd}.xlsx"
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
        
        var lblCustomer = new Label { Text = "ط§ظ„ط¹ظ…ظٹظ„:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        cmbCustomer = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadReportAsync();

        btnExport = new Button { Text = "طھطµط¯ظٹط± Excel", Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnExport.Click += async (_, _) => await ExportToExcelAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnExport, btnRefresh, cmbCustomer, lblCustomer });
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

    private class ComboBoxItem
    {
        public string Text { get; set; } = "";
        public object? Value { get; set; }
        public override string ToString() => Text;
    }
}





