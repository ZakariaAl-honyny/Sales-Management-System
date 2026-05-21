using Serilog;
using ClosedXML.Excel;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Desktop.Controls.Reports.Tabs;

public class ProductMovementReportTab : UserControl, IExportableReport
{
    public DataGridView GetDataGridView() => dgv;
    public string GetReportName() => "حركة منتج";

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
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;
        this.RightToLeft = RightToLeft.Yes;

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

        var btnLoad = new Button { Text = "عرض التقرير", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnLoad, ThemeHelper.ButtonType.Secondary);
        btnLoad.Click += async (_, _) => await LoadReportAsync();

        dtpTo = new DateTimePicker { Width = 110, Margin = new Padding(8, 10, 8, 0) };
        dtpTo.Format = DateTimePickerFormat.Short;
        dtpTo.Value = DateTime.Now;

        var lblTo = new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(0, 15, 0, 0), Font = new Font("Segoe UI", 9F) };

        dtpFrom = new DateTimePicker { Width = 110, Margin = new Padding(8, 10, 8, 0) };
        dtpFrom.Format = DateTimePickerFormat.Short;
        dtpFrom.Value = DateTime.Now.AddMonths(-1);

        var lblFrom = new Label { Text = "من:", AutoSize = true, Margin = new Padding(0, 15, 0, 0), Font = new Font("Segoe UI", 9F) };

        cmbProduct = new ComboBox { Width = 200, Margin = new Padding(8, 10, 8, 0), DropDownStyle = ComboBoxStyle.DropDownList };
        var lblProduct = new Label { Text = "المنتج:", AutoSize = true, Margin = new Padding(0, 15, 0, 0), Font = new Font("Segoe UI", 9F) };

        toolbar.Controls.AddRange(new Control[] { btnLoad, dtpTo, lblTo, dtpFrom, lblFrom, cmbProduct, lblProduct });
        topPanel.Controls.Add(toolbar);

        dgv = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgv);
        dgv.AutoGenerateColumns = false;
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "التاريخ", DataPropertyName = "MovementDate", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "النوع", DataPropertyName = "MovementType", Width = 100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Warehouse", HeaderText = "المخزن", DataPropertyName = "WarehouseName", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyChange", HeaderText = "الكمية", DataPropertyName = "QuantityChange", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Before", HeaderText = "قبل", DataPropertyName = "QuantityBefore", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "After", HeaderText = "بعد", DataPropertyName = "QuantityAfter", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "المرجع", DataPropertyName = "ReferenceType", Width = 100 });

        lblTotal = new Label
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

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgv, 0, 1);
        mainLayout.Controls.Add(lblTotal, 0, 2);

        this.Controls.Add(mainLayout);
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
                Text = "جارٍ التحميل...",
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
            _notification.ShowWarning("يرجى اختيار منتج");
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
                lblTotal.Text = $"إجمالي الحركات: {data.Count}";
            }
            else
            {
                _notification.ShowError(result.Error ?? "حدث خطأ");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل تقرير حركة المنتج");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            ShowLoading(false);
        }
    }
}







