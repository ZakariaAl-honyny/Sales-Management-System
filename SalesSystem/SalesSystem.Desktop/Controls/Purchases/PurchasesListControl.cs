using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Purchases;

public partial class PurchasesListControl : UserControl
{
    private readonly IPurchaseInvoiceApiService _purchaseApi;
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
    private DataGridView dgvPurchases = null!;
    private Label lblStatusLabel = null!;

    public PurchasesListControl(
        IPurchaseInvoiceApiService purchaseApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification,
        ISessionService session)
    {
        _purchaseApi = purchaseApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        _session = session;
        
        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        this.RightToLeft = RightToLeft.Yes;
        dgvPurchases.DataSource = _bindingSource;
        dgvPurchases.AutoGenerateColumns = false;
        dgvPurchases.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPurchases.MultiSelect = false;
        dgvPurchases.ReadOnly = true;
        dgvPurchases.AllowUserToAddRows = false;
        dgvPurchases.BackgroundColor = Color.White;
        dgvPurchases.BorderStyle = BorderStyle.None;

        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 130 });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusName", HeaderText = "الحالة", Width = 100 });

        dgvPurchases.CellFormatting += (s, e) => {
            if (dgvPurchases.Columns[e.ColumnIndex].DataPropertyName == "StatusName" && e.Value != null) {
                e.CellStyle.ForeColor = e.Value.ToString() switch { "مرحل" => Color.Green, "ملغي" => Color.Red, _ => Color.Blue };
            }
        };
        dgvPurchases.DoubleClick += (_, _) => btnView.PerformClick();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<PurchaseInvoiceChangedMessage>(async _ => await LoadPurchasesAsync());
        await LoadPurchasesAsync();
    }

    private async Task LoadPurchasesAsync()
    {
        try {
            SetBusy(true);
            byte? status = cmbStatus.SelectedIndex > 0 ? (byte?)cmbStatus.SelectedIndex : null;
            var result = await _purchaseApi.GetAllAsync(txtSearch.Text, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1), status);
            if (result.IsSuccess) {
                _bindingSource.DataSource = result.Value!.Select(p => new {
                    p.Id, p.InvoiceNo, p.InvoiceDate, SupplierName = p.SupplierName, p.TotalAmount,
                    StatusName = p.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "غير معروف" },
                    Original = p
                }).ToList();
                lblStatusLabel.Text = $"عدد الفواتير: {result.Value!.Count}";
            } else _notification.ShowError(result.Error!);
        } catch (Exception ex) { _notification.ShowError("خطأ: " + ex.Message); }
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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        var top = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.RightToLeft };
        dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };
        cmbStatus = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbStatus.Items.AddRange(new object[] { "الكل", "مسودة", "مرحل", "ملغي" });
        cmbStatus.SelectedIndex = 0;
        txtSearch = new TextBox { Width = 150, PlaceholderText = "بحث..." };
        btnSearch = new Button { Text = "بحث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadPurchasesAsync();
        btnRefresh = new Button { Text = "تحديث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadPurchasesAsync();

        filters.Controls.AddRange(new Control[] { btnRefresh, btnSearch, txtSearch, cmbStatus, new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, dtpTo, new Label { Text = "إلى:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) }, dtpFrom, new Label { Text = "من:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) } });

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
        btnAdd = new Button { Text = "فاتورة جديدة", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAdd.Click += (_, _) => ShowEditor();
        btnView = new Button { Text = "عرض / تعديل", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White };
        btnView.Click += (_, _) => { if (dgvPurchases.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowEditor(d.Original); } };
        btnPost = new Button { Text = "ترحيل", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.SeaGreen, ForeColor = Color.White };
        btnPost.Click += async (_, _) => await PostSelectedAsync();
        btnCancel = new Button { Text = "إلغاء الفاتورة", Width = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White };
        btnCancel.Click += async (_, _) => await CancelSelectedAsync();

        actions.Controls.AddRange(new Control[] { btnCancel, btnPost, btnView, btnAdd });
        top.Controls.Add(filters); top.Controls.Add(actions);

        dgvPurchases = new DataGridView { Dock = DockStyle.Fill };
        lblStatusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        layout.Controls.Add(top, 0, 0); layout.Controls.Add(dgvPurchases, 0, 1); layout.Controls.Add(lblStatusLabel, 0, 2);
        this.Controls.Add(layout);
    }

    private void ShowEditor(PurchaseInvoiceDto? p = null) {
        var editor = ActivatorUtilities.CreateInstance<PurchaseInvoiceForm>(_serviceProvider, p ?? (object)Type.Missing);
        editor.ShowDialog();
    }

    private async Task PostSelectedAsync() {
        if (dgvPurchases.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        PurchaseInvoiceDto p = d.Original;
        if (p.Status != 1) return;
        if (MessageBox.Show($"ترحيل الفاتورة {p.InvoiceNo}؟", "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var res = await _purchaseApi.PostAsync(p.Id);
        if (res.IsSuccess) {
            _notification.ShowSuccess("تم الترحيل");
            _eventBus.Publish(new PurchaseInvoiceChangedMessage(p.Id));
            foreach (var item in p.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
        } else _notification.ShowError(res.Error!);
    }

    private async Task CancelSelectedAsync() {
        if (dgvPurchases.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        PurchaseInvoiceDto p = d.Original;
        if (p.Status == 3) return;
        if (MessageBox.Show($"إلغاء الفاتورة {p.InvoiceNo}؟", "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var res = await _purchaseApi.CancelAsync(p.Id);
        if (res.IsSuccess) {
            _notification.ShowSuccess("تم الإلغاء");
            _eventBus.Publish(new PurchaseInvoiceChangedMessage(p.Id));
            foreach (var item in p.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
        } else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
