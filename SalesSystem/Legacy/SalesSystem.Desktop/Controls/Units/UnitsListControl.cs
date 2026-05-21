using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Units;

[System.ComponentModel.DesignerCategory("Code")]
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
            Log.Error(ex, "حدث خطأ في تحميل قائمة الوحدات");
            _notification.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات. تم تسجيل التفاصيل للدعم الفني.");
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

        btnAdd = new Button { Text = "وحدة جديدة", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        btnEdit.Click += (_, _) => {
            if (dgvUnits.CurrentRow?.DataBoundItem is UnitDto u) ShowEditor(u);
        };

        btnDelete = new Button { Text = "حذف / تعطيل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnDelete, ThemeHelper.ButtonType.Ghost);
        btnDelete.ForeColor = ThemeHelper.Danger;
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadUnitsAsync(); };

        txtSearch = new TextBox { Width = 250, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "ابحث باسم الوحدة...";

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh, txtSearch });
        topPanel.Controls.Add(toolbar);

        dgvUnits = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvUnits);
        
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
        mainLayout.Controls.Add(dgvUnits, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);

        this.Controls.Add(mainLayout);
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
        ActivatorUtilities.CreateInstance<UnitDialog>(_sp, new object[] { u! });
}



