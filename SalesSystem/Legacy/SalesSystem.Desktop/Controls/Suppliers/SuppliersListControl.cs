using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Desktop.Forms;
using SalesSystem.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace SalesSystem.Desktop.Controls.Suppliers;

[System.ComponentModel.DesignerCategory("Code")]
public class SuppliersListControl : UserControl
{
    private readonly ISupplierApiService _supplierApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionService _session;
    private IDisposable? _subscription;
    private readonly BindingSource _bindingSource = new();

    private DataGridView dgvSuppliers = null!;
    private TextBox txtSearch = null!;
    private CheckBox chkShowInactive = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDeactivate = null!;
    private Button btnReactivate = null!;
    private Label lblStatus = null!;

    public SuppliersListControl(
        ISupplierApiService supplierApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification,
        ISessionService session)
    {
        _supplierApi = supplierApi;
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
        dgvSuppliers.AutoGenerateColumns = false;
        dgvSuppliers.DataSource = _bindingSource;
        dgvSuppliers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSuppliers.MultiSelect = false;
        dgvSuppliers.ReadOnly = true;
        dgvSuppliers.AllowUserToAddRows = false;
        
        ThemeHelper.ApplyDataGridViewStyle(dgvSuppliers);

        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 100 });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = "رقم الهاتف", Width = 150 });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CurrentBalance", HeaderText = "الرصيد الحالي", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvSuppliers.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 80 });

        dgvSuppliers.CellFormatting += (s, e) => {
            if (dgvSuppliers.Columns[e.ColumnIndex].DataPropertyName == "CurrentBalance" && e.Value is decimal balance) {
                if (balance > 0) e.CellStyle.ForeColor = Color.Red;
                else if (balance < 0) e.CellStyle.ForeColor = Color.Green;
                else e.CellStyle.ForeColor = Color.Gray;
                e.CellStyle.Font = new Font(dgvSuppliers.Font, FontStyle.Bold);
            }
        };
        dgvSuppliers.DoubleClick += (_, _) => { if (_session.Current?.Role <= UserRole.Manager) btnEdit.PerformClick(); };
    }

    private void ApplyPermissions()
    {
        bool canManage = _session.Current?.Role <= UserRole.Manager;
        btnAdd.Visible = canManage;
        btnEdit.Visible = canManage;
        btnDeactivate.Visible = canManage;
        btnReactivate.Visible = canManage;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.RightToLeft = RightToLeft.Yes;
        _subscription = _eventBus.Subscribe<SupplierChangedMessage>(async _ => await LoadSuppliersAsync());
        await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try {
            SetBusy(true);
            var result = await _supplierApi.GetAllAsync(txtSearch.Text, chkShowInactive.Checked);
            if (result.IsSuccess) {
                _bindingSource.DataSource = result.Value;
                lblStatus.Text = $"إجمالي الموردين: {result.Value!.Count}";
            } else _notification.ShowError(result.Error!);
        }
        catch (Exception ex) { 
            Log.Error(ex, "حدث خطأ في تحميل قائمة الموردين");
            _notification.ShowError("خطأ في تحميل الموردين. تم تسجيل التفاصيل للدعم الفني."); 
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

        btnAdd = new Button { Text = "مورد جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        btnEdit.Click += (_, _) => { if (dgvSuppliers.CurrentRow?.DataBoundItem is SupplierDto s) ShowEditor(s); };

        btnDeactivate = new Button { Text = "تعطيل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnDeactivate, ThemeHelper.ButtonType.Ghost);
        btnDeactivate.ForeColor = ThemeHelper.Danger;
        btnDeactivate.Click += async (_, _) => await ToggleStatusAsync(true);

        btnReactivate = new Button { Text = "تنشيط", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnReactivate, ThemeHelper.ButtonType.Ghost);
        btnReactivate.Click += async (_, _) => await ToggleStatusAsync(false);

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadSuppliersAsync(); };

        chkShowInactive = new CheckBox { 
            Text = "إظهار غير النشط", 
            AutoSize = true, 
            Margin = new Padding(15, 8, 15, 0),
            Font = new Font("Segoe UI", 9F) 
        };
        chkShowInactive.CheckedChanged += async (_, _) => await LoadSuppliersAsync();

        txtSearch = new TextBox { Width = 250, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "بحث باسم أو كود المورد...";
        txtSearch.TextChanged += async (_, _) => await LoadSuppliersAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDeactivate, btnReactivate, btnRefresh, chkShowInactive, txtSearch });
        topPanel.Controls.Add(toolbar);

        dgvSuppliers = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvSuppliers);
        
        lblStatus = new Label { 
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
        mainLayout.Controls.Add(dgvSuppliers, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);
        
        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(SupplierDto? s = null) {
        var dialog = _serviceProvider.GetRequiredService<SupplierEditorForm>();
        dialog.LoadData(s);
        dialog.ShowDialog();
    }

    private async Task ToggleStatusAsync(bool deactivate) {
        if (dgvSuppliers.CurrentRow?.DataBoundItem is not SupplierDto s) return;
        string action = deactivate ? "تعطيل" : "تنشيط";
        if (MessageBox.Show($"هل تريد {action} المورد {s.Name}؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var res = deactivate ? await _supplierApi.DeactivateAsync(s.Id) : await _supplierApi.ReactivateAsync(s.Id);
        if (res.IsSuccess) {
            _notification.ShowSuccess($"تم {action} المورد بنجاح");
            _eventBus.Publish(new SupplierChangedMessage(s.Id));
        } else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
