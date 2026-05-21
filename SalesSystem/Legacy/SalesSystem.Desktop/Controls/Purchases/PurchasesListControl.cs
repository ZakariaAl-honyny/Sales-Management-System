using Serilog;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace SalesSystem.Desktop.Controls.Purchases;

[System.ComponentModel.DesignerCategory("Code")]
public class PurchasesListControl : UserControl
{
    private readonly IPurchaseInvoiceApiService _purchaseApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _subscription;
    private readonly BindingSource _bindingSource = new();

    private DataGridView dgvPurchases = null!;
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

    public PurchasesListControl(
        IPurchaseInvoiceApiService purchaseApi,
        INotificationService notification,
        IEventBus eventBus,
        IServiceProvider serviceProvider)
    {
        _purchaseApi = purchaseApi;
        _notification = notification;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;

        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        dgvPurchases.AutoGenerateColumns = false;
        dgvPurchases.DataSource = _bindingSource;
        dgvPurchases.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvPurchases.MultiSelect = false;
        dgvPurchases.ReadOnly = true;
        dgvPurchases.AllowUserToAddRows = false;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvPurchases);

        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 130 });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount", HeaderText = "الإجمالي", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvPurchases.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusName", HeaderText = "الحالة", Width = 100 });

        dgvPurchases.CellFormatting += (s, e) => {
            if (dgvPurchases.Columns[e.ColumnIndex].DataPropertyName == "StatusName" && e.Value != null) {
                e.CellStyle.ForeColor = e.Value.ToString() switch { "مرحل" => Color.Green, "ملغي" => Color.Red, _ => Color.Blue };
                e.CellStyle.Font = new Font(dgvPurchases.Font, FontStyle.Bold);
            }
        };
        dgvPurchases.DoubleClick += (_, _) => btnView.PerformClick();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.RightToLeft = RightToLeft.Yes;
        _subscription = _eventBus.Subscribe<PurchaseInvoiceChangedMessage>(async _ => await LoadPurchasesAsync());
        await LoadPurchasesAsync();
    }

    private async Task LoadPurchasesAsync()
    {
        try
        {
            SetBusy(true);
            byte? status = cmbStatus.SelectedIndex > 0 ? (byte?)cmbStatus.SelectedIndex : null;
            var result = await _purchaseApi.GetAllAsync(txtSearch.Text, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1), status);
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value!.Select(p => new
                {
                    p.Id,
                    p.InvoiceNo,
                    p.InvoiceDate,
                    SupplierName = p.SupplierName,
                    p.TotalAmount,
                    StatusName = p.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "غير معروف" },
                    Original = p
                }).ToList();
                lblStatusLabel.Text = $"إجمالي الفواتير: {result.Value!.Count}";
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل قائمة المشتريات");
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
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
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        cmbStatus = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbStatus.Items.AddRange(new object[] { "الكل", "مسودة", "مرحل", "ملغي" });
        cmbStatus.SelectedIndex = 0;
        
        txtSearch = new TextBox { Width = 200 };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "رقم الفاتورة...";

        btnSearch = new Button { Text = "بحث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnSearch, ThemeHelper.ButtonType.Secondary);
        btnSearch.Click += async (_, _) => await LoadPurchasesAsync();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await LoadPurchasesAsync();

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

        btnAdd = new Button { Text = "فاتورة مشتريات", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnView = new Button { Text = "عرض / تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnView, ThemeHelper.ButtonType.Secondary);
        btnView.Click += (_, _) => { if (dgvPurchases.CurrentRow?.DataBoundItem is object obj) { dynamic d = obj; ShowEditor(d.Original); } };

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

        dgvPurchases = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvPurchases);
        
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
        mainLayout.Controls.Add(dgvPurchases, 0, 1);
        mainLayout.Controls.Add(lblStatusLabel, 0, 2);
        
        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(PurchaseInvoiceDto? p = null) {
        var editor = _serviceProvider.GetRequiredService<PurchaseInvoiceForm>();
        editor.LoadData(p);
        editor.ShowDialog();
    }

    private async Task PostSelectedAsync() {
        if (dgvPurchases.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        PurchaseInvoiceDto p = d.Original;
        if (p.Status != 1) {
            _notification.ShowWarning("يمكن ترحيل الفواتير المسودة فقط");
            return;
        }
        if (MessageBox.Show($"هل تريد ترحيل الفاتورة رقم {p.InvoiceNo}؟", "تأكيد الترحيل", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            var res = await _purchaseApi.PostAsync(p.Id);
            if (res.IsSuccess) {
                _notification.ShowSuccess("تم الترحيل بنجاح");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(p.Id));
                foreach (var item in p.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
            } else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ أثناء ترحيل فاتورة المشتريات {InvoiceNo}", p.InvoiceNo);
            _notification.ShowError("حدث خطأ أثناء الترحيل. تم تسجيل التفاصيل.");
        }
    }

    private async Task CancelSelectedAsync() {
        if (dgvPurchases.CurrentRow?.DataBoundItem is not object obj) return; dynamic d = obj;
        PurchaseInvoiceDto p = d.Original;
        if (p.Status == 3) return;
        if (MessageBox.Show($"هل تريد إلغاء الفاتورة رقم {p.InvoiceNo}؟", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            var res = await _purchaseApi.CancelAsync(p.Id);
            if (res.IsSuccess) {
                _notification.ShowSuccess("تم الإلغاء بنجاح");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(p.Id));
                foreach (var item in p.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
            } else _notification.ShowError(res.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ أثناء إلغاء فاتورة المشتريات {InvoiceNo}", p.InvoiceNo);
            _notification.ShowError("حدث خطأ أثناء الإلغاء. تم تسجيل التفاصيل.");
        }
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
