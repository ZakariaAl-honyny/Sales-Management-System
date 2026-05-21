using Serilog;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace SalesSystem.Desktop.Controls.Sales;

[System.ComponentModel.DesignerCategory("Code")]
public class SalesListControl : UserControl
{
    private readonly ISalesInvoiceApiService _salesApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionService _session;
    private IDisposable? _subscription;
    private readonly BindingSource _bindingSource = new();

    private DataGridView dgvSales = null!;
    private TextBox txtSearch = null!;
    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private ComboBox cmbStatus = null!;
    private Button btnSearch = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnView = null!;
    private Button btnPost = null!;
    private Button btnCancel = null!;
    private Label lblStatusLabel = null!;

    public SalesListControl(
        ISalesInvoiceApiService salesApi,
        INotificationService notification,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        ISessionService session)
    {
        _salesApi = salesApi;
        _notification = notification;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _session = session;

        InitializeComponent();
        SetupGrid();
        ApplyPermissions();
    }

    private void SetupGrid()
    {
        dgvSales.AutoGenerateColumns = false;
        dgvSales.DataSource = _bindingSource;
        dgvSales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSales.MultiSelect = false;
        dgvSales.ReadOnly = true;
        dgvSales.AllowUserToAddRows = false;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvSales);

        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 130 });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusName", HeaderText = "الحالة", Width = 100 });

        dgvSales.CellFormatting += (s, e) => {
            if (dgvSales.Columns[e.ColumnIndex].DataPropertyName == "StatusName" && e.Value != null) {
                e.CellStyle.ForeColor = e.Value.ToString() switch { "مرحل" => Color.Green, "ملغي" => Color.Red, _ => Color.Blue };
                e.CellStyle.Font = new Font(dgvSales.Font, FontStyle.Bold);
            }
        };
        dgvSales.DoubleClick += (_, _) => btnView.PerformClick();
    }

    private void ApplyPermissions()
    {
        btnCancel.Visible = _session.Current?.Role <= UserRole.Manager;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.RightToLeft = RightToLeft.Yes;
        _subscription = _eventBus.Subscribe<SaleInvoiceChangedMessage>(async _ => await LoadSalesAsync());
        await LoadSalesAsync();
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            SetBusy(true);
            byte? status = cmbStatus.SelectedIndex > 0 ? (byte?)cmbStatus.SelectedIndex : null;
            var result = await _salesApi.GetAllAsync(txtSearch.Text, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1), status);
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value!.Select(s => new
                {
                    s.Id,
                    s.InvoiceNo,
                    s.InvoiceDate,
                    CustomerName = s.CustomerName ?? "عميل نقدي",
                    s.TotalAmount,
                    StatusName = s.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "غير معروف" },
                    Original = s
                }).ToList();
                lblStatusLabel.Text = $"إجمالي الفواتير: {result.Value!.Count}";
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل قائمة المبيعات");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally { SetBusy(false); }
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // Multi-row toolbar for filters/actions
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var toolbarLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // Row 1: Filters
        var filtersPanel = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        dtpFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        dtpTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        cmbStatus = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbStatus.Items.AddRange(new object[] { "الكل", "مسودة", "مرحل", "ملغي" });
        cmbStatus.SelectedIndex = 0;
        
        txtSearch = new TextBox { Width = 200 };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "رقم الفاتورة...";

        btnSearch = new Button { Text = "بحث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnSearch, ThemeHelper.ButtonType.Secondary);
        btnSearch.Click += async (_, _) => await LoadSalesAsync();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await LoadSalesAsync();

        filtersPanel.Controls.AddRange(new Control[] { 
            btnRefresh, btnSearch, txtSearch, 
            new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(5, 8, 5, 0) }, cmbStatus,
            new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(5, 8, 5, 0) }, dtpTo,
            new Label { Text = "من:", AutoSize = true, Margin = new Padding(5, 8, 5, 0) }, dtpFrom 
        });

        // Row 2: Actions
        var actionsPanel = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        btnAdd = new Button { Text = "فاتورة جديدة", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnView = new Button { Text = "عرض / تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnView, ThemeHelper.ButtonType.Secondary);
        btnView.Click += (_, _) => { if (dgvSales.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowEditor(d.Original); } };

        btnPost = new Button { Text = "ترحيل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnPost, ThemeHelper.ButtonType.Ghost);
        btnPost.Click += async (_, _) => await PostSelectedAsync();

        btnCancel = new Button { Text = "إلغاء", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnCancel, ThemeHelper.ButtonType.Ghost);
        btnCancel.ForeColor = ThemeHelper.Danger;
        btnCancel.Click += async (_, _) => await CancelSelectedAsync();

        actionsPanel.Controls.AddRange(new Control[] { btnAdd, btnView, btnPost, btnCancel });

        toolbarLayout.Controls.Add(filtersPanel, 0, 0);
        toolbarLayout.Controls.Add(actionsPanel, 0, 1);
        topPanel.Controls.Add(toolbarLayout);

        dgvSales = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvSales);
        
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
        mainLayout.Controls.Add(dgvSales, 0, 1);
        mainLayout.Controls.Add(lblStatusLabel, 0, 2);
        
        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(SalesInvoiceDto? s = null) {
        var editor = _serviceProvider.GetRequiredService<SalesInvoiceForm>();
        editor.LoadData(s);
        editor.ShowDialog();
    }

    private async Task PostSelectedAsync() {
        if (dgvSales.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        SalesInvoiceDto s = d.Original;
        if (s.Status != 1) return;
        if (MessageBox.Show($"هل تريد ترحيل الفاتورة رقم {s.InvoiceNo}؟", "تأكيد الترحيل", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            var res = await _salesApi.PostAsync(s.Id);
            if (res.IsSuccess) {
                _notification.ShowSuccess("تم الترحيل بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(s.Id));
                foreach (var item in s.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
            } else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ أثناء ترحيل الفاتورة {InvoiceNo}", s.InvoiceNo);
            _notification.ShowError("حدث خطأ أثناء الترحيل. تم تسجيل التفاصيل.");
        }
    }

    private async Task CancelSelectedAsync() {
        if (dgvSales.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        SalesInvoiceDto s = d.Original;
        if (s.Status == 3) return;
        if (MessageBox.Show($"هل تريد إلغاء الفاتورة رقم {s.InvoiceNo}؟", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            var res = await _salesApi.CancelAsync(s.Id);
            if (res.IsSuccess) {
                _notification.ShowSuccess("تم الإلغاء بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(s.Id));
                foreach (var item in s.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
            } else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ أثناء إلغاء الفاتورة {InvoiceNo}", s.InvoiceNo);
            _notification.ShowError("حدث خطأ أثناء الإلغاء. تم تسجيل التفاصيل.");
        }
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
