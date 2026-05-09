using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Sales;

public partial class SalesListControl : UserControl
{
    private readonly ISalesInvoiceApiService _salesApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly ISessionService _session;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

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
    private DataGridView dgvSales = null!;
    private Label lblStatusLabel = null!;

    public SalesListControl(
        ISalesInvoiceApiService salesApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification,
        ISessionService session)
    {
        _salesApi = salesApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        _session = session;
        
        InitializeComponent();
        SetupGrid();
        ApplyPermissions();
    }

    private void SetupGrid()
    {
        this.RightToLeft = RightToLeft.Yes;
        dgvSales.DataSource = _bindingSource;
        dgvSales.AutoGenerateColumns = false;
        dgvSales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSales.MultiSelect = false;
        dgvSales.ReadOnly = true;
        dgvSales.AllowUserToAddRows = false;
        dgvSales.BackgroundColor = Color.White;
        dgvSales.BorderStyle = BorderStyle.None;

        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 130 });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusName", HeaderText = "الحالة", Width = 100 });

        dgvSales.CellFormatting += DgvSales_CellFormatting;
        dgvSales.DoubleClick += (_, _) => btnView.PerformClick();
    }

    private void DgvSales_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (dgvSales.Columns[e.ColumnIndex].DataPropertyName == "StatusName" && e.Value != null)
        {
            string status = e.Value.ToString()!;
            e.CellStyle.ForeColor = status switch {
                "مرحل" => Color.Green,
                "ملغي" => Color.Red,
                _ => Color.Blue
            };
        }
    }

    private void ApplyPermissions()
    {
        btnCancel.Visible = _session.Current?.Role <= UserRole.Manager;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
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
                _bindingSource.DataSource = result.Value!.Select(s => new {
                    s.Id,
                    s.InvoiceNo,
                    s.InvoiceDate,
                    CustomerName = s.CustomerName ?? "عميل نقدي",
                    s.TotalAmount,
                    StatusName = s.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "غير معروف" },
                    Original = s
                }).ToList();
                lblStatusLabel.Text = $"عدد الفواتير: {result.Value!.Count}";
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            _notification.ShowError("حدث خطأ أثناء تحميل البيانات: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        txtSearch.Enabled = !busy;
        dtpFrom.Enabled = !busy;
        dtpTo.Enabled = !busy;
        cmbStatus.Enabled = !busy;
        btnSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAdd.Enabled = !busy;
        btnView.Enabled = !busy;
        btnPost.Enabled = !busy;
        btnCancel.Enabled = !busy;
        dgvSales.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        var topPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        
        var filterFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.RightToLeft };
        dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };
        dtpFrom.Value = DateTime.Today.AddDays(-7);
        dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };
        cmbStatus = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbStatus.Items.AddRange(new object[] { "الكل", "مسودة", "مرحل", "ملغي" });
        cmbStatus.SelectedIndex = 0;
        txtSearch = new TextBox { Width = 150, PlaceholderText = "بحث..." };
        btnSearch = new Button { Text = "بحث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadSalesAsync();
        btnRefresh = new Button { Text = "تحديث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadSalesAsync();

        filterFlow.Controls.AddRange(new Control[] { btnRefresh, btnSearch, txtSearch, cmbStatus, new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, dtpTo, new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, dtpFrom, new Label { Text = "من:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) } });

        var actionFlow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
        btnAdd = new Button { Text = "فاتورة جديدة", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAdd.Click += (_, _) => ShowEditor();
        btnView = new Button { Text = "عرض / تعديل", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White };
        btnView.Click += (_, _) => {
            if (dgvSales.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowEditor(d.Original); }
        };
        btnPost = new Button { Text = "ترحيل", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.SeaGreen, ForeColor = Color.White };
        btnPost.Click += async (_, _) => await PostSelectedAsync();
        btnCancel = new Button { Text = "إلغاء الفاتورة", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White };
        btnCancel.Click += async (_, _) => await CancelSelectedAsync();

        actionFlow.Controls.AddRange(new Control[] { btnCancel, btnPost, btnView, btnAdd });

        topPanel.Controls.Add(filterFlow);
        topPanel.Controls.Add(actionFlow);

        dgvSales = new DataGridView { Dock = DockStyle.Fill };
        lblStatusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvSales, 0, 1);
        mainLayout.Controls.Add(lblStatusLabel, 0, 2);

        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(SalesInvoiceDto? s = null)
    {
        var editor = ActivatorUtilities.CreateInstance<SalesInvoiceForm>(_serviceProvider, s ?? (object)Type.Missing);
        editor.ShowDialog();
    }

    private async Task PostSelectedAsync()
    {
        if (dgvSales.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        SalesInvoiceDto s = d.Original;
        if (s.Status != (byte)InvoiceStatus.Draft) return;

        if (MessageBox.Show($"هل تريد ترحيل الفاتورة رقم {s.InvoiceNo}؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var res = await _salesApi.PostAsync(s.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess("تم الترحيل بنجاح");
            _eventBus.Publish(new SaleInvoiceChangedMessage(s.Id));
            // Publish StockChangedMessage for each item as requested in T027 context
            foreach (var item in s.Items)
            {
                _eventBus.Publish(new StockChangedMessage(item.ProductId));
            }
        }
        else _notification.ShowError(res.Error!);
    }

    private async Task CancelSelectedAsync()
    {
        if (dgvSales.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        SalesInvoiceDto s = d.Original;
        
        if (s.Status == (byte)InvoiceStatus.Cancelled) return;
        if (MessageBox.Show($"هل تريد إلغاء الفاتورة رقم {s.InvoiceNo}؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var res = await _salesApi.CancelAsync(s.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess("تم الإلغاء بنجاح");
            _eventBus.Publish(new SaleInvoiceChangedMessage(s.Id));
            foreach (var item in s.Items)
            {
                _eventBus.Publish(new StockChangedMessage(item.ProductId));
            }
        }
        else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
