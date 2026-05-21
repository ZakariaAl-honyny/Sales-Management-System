using Serilog;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using ClosedXML.Excel;
using SalesSystem.Desktop.Helpers;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class LowStockReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgvReport;
    public string GetReportName() => "تنبيه المخزون المنخفض";

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
            Log.Error(ex, "حدث خطأ في تحميل تقرير التنبيهات للمخزون المنخفض");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل التقرير. تم تسجيل التفاصيل للدعم الفني.");
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
            lblSummary.Text = "جميع الأصناف فوق الحد الأدنى للمخزون";
            return;
        }

        var totalQty = list.Sum(x => x.Quantity);
        lblSummary.Text = $"عدد الأصناف أقل من الحد الأدنى: {list.Count} | إجمالي الكمية: {totalQty:N3}";
    }

    private void FormatGrid()
    {
        if (dgvReport.Columns.Count == 0) return;

        var hides = new[] { "ProductId", "WarehouseId" };
        foreach (var h in hides)
        {
            if (dgvReport.Columns.Contains(h)) dgvReport.Columns[h].Visible = false;
        }

        SetHeader("ProductCode", "كود المنتج");
        SetHeader("ProductName", "اسم المنتج");
        SetHeader("UnitName", "الوحدة");
        SetHeader("WarehouseName", "المستودع");
        SetHeader("Quantity", "الكمية الحالية");
        SetHeader("ReorderLevel", "الحد الأدنى");

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
            _notification.ShowWarning("لا توجد بيانات للتصدير");
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تنبيه المخزون المنخفض");

            var headers = new[] { "كود المنتج", "اسم المنتج", "الوحدة", "المستودع", "الكمية الحالية", "الحد الأدنى" };
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
                FileName = $"تنبيه_المخزون_المنخفض_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                workbook.SaveAs(dialog.FileName);
                _notification.ShowSuccess("تم تصدير التقرير بنجاح");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تصدير تقرير التنبيهات للمخزون المنخفض إلى Excel");
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

        btnRefresh = new Button { Text = "تحديث البيانات", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Secondary);
        btnRefresh.Click += async (_, _) => await LoadReportAsync();

        btnExport = new Button { Text = "تصدير Excel", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnExport, ThemeHelper.ButtonType.Ghost);
        btnExport.ForeColor = Color.FromArgb(46, 125, 50); // Professional Green
        btnExport.Click += async (_, _) => await ExportToExcelAsync();

        toolbar.Controls.AddRange(new Control[] { btnRefresh, btnExport });
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
            Padding = new Padding(0),
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
