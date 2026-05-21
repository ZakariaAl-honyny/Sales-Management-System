using Serilog;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using ClosedXML.Excel;
using SalesSystem.Desktop.Helpers;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class SalesReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgvReport;
    public string GetReportName() => "تقرير المبيعات";

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

    public SalesReportTab(IReportApiService reportApi, INotificationService notification)
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
            var result = await _reportApi.GetSalesAsync(dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1));
            
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
            Log.Error(ex, "خطأ في تحميل تقرير المبيعات");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل التقرير. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void UpdateSummary(List<SalesReportDto> list)
    {
        if (list.Count == 0)
        {
            lblSummary.Text = "لا توجد بيانات";
            return;
        }

        var totalSales = list.Sum(x => x.TotalAmount);
        var totalPaid = list.Sum(x => x.PaidAmount);
        var totalDue = list.Sum(x => x.DueAmount);

        lblSummary.Text = $"عدد الفواتير: {list.Count} | إجمالي المبيعات: {totalSales:N2} | المدفوع: {totalPaid:N2} | المتبقي: {totalDue:N2}";
    }

    private void FormatGrid()
    {
        if (dgvReport.Columns.Count == 0) return;

        var hides = new[] { "InvoiceId", "WarehouseName", "SubTotal", "DiscountAmount", "TaxAmount" };
        foreach (var h in hides)
        {
            if (dgvReport.Columns.Contains(h)) dgvReport.Columns[h].Visible = false;
        }

        SetHeader("InvoiceNo", "رقم الفاتورة");
        SetHeader("InvoiceDate", "التاريخ");
        SetHeader("CustomerName", "العميل");
        SetHeader("TotalAmount", "الإجمالي");
        SetHeader("PaidAmount", "المدفوع");
        SetHeader("DueAmount", "المتبقي");

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
        if (_bindingSource.DataSource is not List<SalesReportDto> data || data.Count == 0)
        {
            _notification.ShowWarning("لا توجد بيانات للتصدير");
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير المبيعات");

            // Headers
            var headers = new[] { "رقم الفاتورة", "التاريخ", "العميل", "الإجمالي", "المدفوع", "المتبقي" };
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
                worksheet.Cell(row + 2, 3).Value = item.CustomerName ?? "-";
                worksheet.Cell(row + 2, 4).Value = item.TotalAmount;
                worksheet.Cell(row + 2, 5).Value = item.PaidAmount;
                worksheet.Cell(row + 2, 6).Value = item.DueAmount;
            }

            worksheet.Columns().AdjustToContents();

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تقرير_المبيعات_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                workbook.SaveAs(dialog.FileName);
                _notification.ShowSuccess("تم تصدير التقرير بنجاح");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطأ في تصدير تقرير المبيعات إلى Excel");
            _notification.ShowError("حدث خطأ أثناء التصدير. تم تسجيل التفاصيل للدعم الفني.");
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

        btnRefresh = new Button { Text = "تحديث التقرير", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Secondary);
        btnRefresh.Click += async (_, _) => await LoadReportAsync();

        btnExport = new Button { Text = "تصدير Excel", Margin = new Padding(8, 0, 15, 0) };
        ThemeHelper.ApplyButtonStyle(btnExport, ThemeHelper.ButtonType.Ghost);
        btnExport.ForeColor = Color.FromArgb(46, 125, 50); // Professional Green
        btnExport.Click += async (_, _) => await ExportToExcelAsync();

        dtpTo = new DateTimePicker { Width = 110, Margin = new Padding(8, 10, 8, 0) };
        dtpTo.Format = DateTimePickerFormat.Short;
        dtpTo.Value = DateTime.Today;

        var lblTo = new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(0, 15, 0, 0), Font = new Font("Segoe UI", 9F) };

        dtpFrom = new DateTimePicker { Width = 110, Margin = new Padding(8, 10, 8, 0) };
        dtpFrom.Format = DateTimePickerFormat.Short;
        dtpFrom.Value = DateTime.Today.AddMonths(-1);

        var lblFrom = new Label { Text = "من:", AutoSize = true, Margin = new Padding(0, 15, 0, 0), Font = new Font("Segoe UI", 9F) };

        toolbar.Controls.AddRange(new Control[] { btnRefresh, btnExport, dtpTo, lblTo, dtpFrom, lblFrom });
        topPanel.Controls.Add(toolbar);

        dgvReport = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvReport);

        lblSummary = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "جاهز",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ThemeHelper.TextSecondary,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0)
        };

        loadingPanel = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(100, Color.White) };
        var loadingLabel = new Label { 
            Text = "جارٍ التحميل...", 
            Font = new Font("Segoe UI", 12F, FontStyle.Bold), 
            AutoSize = true,
            ForeColor = ThemeHelper.Primary
        };
        loadingLabel.Location = new Point((Width - loadingLabel.Width) / 2, (Height - loadingLabel.Height) / 2);
        loadingPanel.Controls.Add(loadingLabel);

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvReport, 0, 1);
        mainLayout.Controls.Add(lblSummary, 0, 2);

        this.Controls.Add(mainLayout);
        this.Controls.Add(loadingPanel);
    }
}
