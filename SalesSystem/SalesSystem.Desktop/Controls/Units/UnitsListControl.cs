using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Units;

public partial class UnitsListControl : UserControl
{
    private readonly IUnitApiService _unitApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private DataGridView dgvUnits = null!;
    private Label lblStatus = null!;

    public UnitsListControl(
        IUnitApiService unitApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _unitApi = unitApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        
        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        this.RightToLeft = RightToLeft.Yes;
        dgvUnits.DataSource = _bindingSource;
        dgvUnits.AutoGenerateColumns = false;
        dgvUnits.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvUnits.MultiSelect = false;
        dgvUnits.ReadOnly = true;
        dgvUnits.AllowUserToAddRows = false;
        dgvUnits.BackgroundColor = Color.White;
        dgvUnits.BorderStyle = BorderStyle.None;

        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Symbol", HeaderText = "الرمز", Width = 100 });
        dgvUnits.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 80 });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<UnitChangedMessage>(async _ => await LoadUnitsAsync());
        await LoadUnitsAsync();
    }

    private async Task LoadUnitsAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _unitApi.GetAllAsync(false); // Using default includeInactive=false
            
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatus.Text = $"عدد الوحدات: {result.Value.Count}";
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
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
        btnSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAdd.Enabled = !busy;
        btnEdit.Enabled = !busy;
        btnDelete.Enabled = !busy;
        dgvUnits.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        
        txtSearch = new TextBox { Width = 250, PlaceholderText = "ابحث باسم الوحدة..." };
        btnSearch = new Button { Text = "بحث", Width = 80, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadUnitsAsync();

        btnRefresh = new Button { Text = "تحديث", Width = 80, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadUnitsAsync(); };

        btnAdd = new Button { Text = "إضافة", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Width = 80, FlatStyle = FlatStyle.Flat };
        btnEdit.Click += (_, _) => {
            if (dgvUnits.CurrentRow?.DataBoundItem is UnitDto u) ShowEditor(u);
        };

        btnDelete = new Button { Text = "حذف/تعطيل", Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.MistyRose };
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnDelete, btnEdit, btnAdd, btnRefresh, btnSearch, txtSearch });
        topPanel.Controls.Add(flow);

        dgvUnits = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        lblStatus = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        this.Controls.Add(dgvUnits);
        this.Controls.Add(lblStatus);
        this.Controls.Add(topPanel);
    }

    private void ShowEditor(UnitDto? u = null)
    {
        var factory = _serviceProvider.GetRequiredService<UnitDialogFactory>();
        var d = factory.Create(u);
        if (d.ShowDialog() == DialogResult.OK) _eventBus.Publish(new UnitChangedMessage(0));
    }

    private async Task DeleteSelectedAsync()
    {
        if (dgvUnits.CurrentRow?.DataBoundItem is not UnitDto u) return;

        var msg = "هل تريد حذف/تعطيل هذه الوحدة؟";
        if (MessageBox.Show(msg, "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var res = await _unitApi.DeleteAsync(u.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess("تمت العملية بنجاح");
            _eventBus.Publish(new UnitChangedMessage(u.Id));
        }
        else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}

public class UnitDialogFactory
{
    private readonly IServiceProvider _sp;
    public UnitDialogFactory(IServiceProvider sp) => _sp = sp;
    public UnitDialog Create(UnitDto? u = null) => 
        ActivatorUtilities.CreateInstance<UnitDialog>(_sp, u ?? (object)Type.Missing);
}

