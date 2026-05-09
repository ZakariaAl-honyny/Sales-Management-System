using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Suppliers;

public partial class SuppliersListControl : UserControl
{
    private readonly ISupplierApiService _supplierApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly ISessionService _session;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

    private TextBox txtSearch = null!;
    private CheckBox chkShowInactive = null!;
    private Button btnSearch = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDeactivate = null!;
    private Button btnReactivate = null!;
    private DataGridView dgvSuppliers = null!;
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
        this.RightToLeft = RightToLeft.Yes;
        dgvSuppliers.DataSource = _bindingSource;
        dgvSuppliers.AutoGenerateColumns = false;
        dgvSuppliers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSuppliers.MultiSelect = false;
        dgvSuppliers.ReadOnly = true;
        dgvSuppliers.AllowUserToAddRows = false;
        dgvSuppliers.BackgroundColor = Color.White;
        dgvSuppliers.BorderStyle = BorderStyle.None;

        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 100 });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = "الهاتف", Width = 120 });
        dgvSuppliers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CurrentBalance", HeaderText = "الرصيد", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        dgvSuppliers.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 80 });

        dgvSuppliers.CellFormatting += DgvSuppliers_CellFormatting;
        dgvSuppliers.DoubleClick += (_, _) => { if (_session.Current?.Role <= UserRole.Manager) btnEdit.PerformClick(); };
    }

    private void DgvSuppliers_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (dgvSuppliers.Columns[e.ColumnIndex].DataPropertyName == "CurrentBalance" && e.Value is decimal balance)
        {
            if (balance > 0) e.CellStyle.ForeColor = Color.Red;
            else if (balance < 0) e.CellStyle.ForeColor = Color.Green;
            else e.CellStyle.ForeColor = Color.Gray;
        }
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
        _subscription = _eventBus.Subscribe<SupplierChangedMessage>(async _ => await LoadSuppliersAsync());
        await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _supplierApi.GetAllAsync(txtSearch.Text, chkShowInactive.Checked);
            
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatus.Text = $"عدد الموردين: {result.Value.Count}";
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
        chkShowInactive.Enabled = !busy;
        btnSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAdd.Enabled = !busy;
        btnEdit.Enabled = !busy;
        btnDeactivate.Enabled = !busy;
        btnReactivate.Enabled = !busy;
        dgvSuppliers.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        
        txtSearch = new TextBox { Width = 200, PlaceholderText = "بحث..." };
        chkShowInactive = new CheckBox { Text = "عرض غير النشط", AutoSize = true, Margin = new Padding(10, 5, 0, 0) };
        btnSearch = new Button { Text = "بحث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadSuppliersAsync();

        btnRefresh = new Button { Text = "تحديث", Width = 70, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadSuppliersAsync(); };

        btnAdd = new Button { Text = "إضافة", Width = 70, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Width = 70, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White };
        btnEdit.Click += (_, _) => {
            if (dgvSuppliers.CurrentRow?.DataBoundItem is SupplierDto s) ShowEditor(s);
        };

        btnDeactivate = new Button { Text = "تعطيل", Width = 70, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White };
        btnDeactivate.Click += async (_, _) => await ToggleStatusAsync(true);

        btnReactivate = new Button { Text = "تفعيل", Width = 70, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.Black };
        btnReactivate.Click += async (_, _) => await ToggleStatusAsync(false);

        topPanel.Controls.AddRange(new Control[] { btnReactivate, btnDeactivate, btnEdit, btnAdd, btnRefresh, btnSearch, chkShowInactive, txtSearch });

        dgvSuppliers = new DataGridView { Dock = DockStyle.Fill };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvSuppliers, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);

        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(SupplierDto? s = null)
    {
        var dialog = ActivatorUtilities.CreateInstance<SupplierEditorForm>(_serviceProvider, s ?? (object)Type.Missing);
        dialog.ShowDialog();
    }

    private async Task ToggleStatusAsync(bool deactivate)
    {
        if (dgvSuppliers.CurrentRow?.DataBoundItem is not SupplierDto s) return;
        
        string action = deactivate ? "تعطيل" : "تفعيل";
        if (MessageBox.Show($"هل تريد {action} هذا المورد؟", "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var res = deactivate ? await _supplierApi.DeactivateAsync(s.Id) : await _supplierApi.ReactivateAsync(s.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess($"تم {action} المورد بنجاح");
            _eventBus.Publish(new SupplierChangedMessage(s.Id));
        }
        else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
