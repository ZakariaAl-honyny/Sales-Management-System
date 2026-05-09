using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Controls.Units;

public partial class UnitManagerDialog : Form
{
    private readonly IUnitApiService _unitApi;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private DataGridView dgvUnits = null!;
    private Button btnAdd = null!;
    private Button btnDelete = null!;
    private Button btnClose = null!;
    private TextBox txtNewName = null!;
    private TextBox txtNewSymbol = null!;

    public UnitManagerDialog(
        IUnitApiService unitApi,
        IEventBus eventBus,
        INotificationService notification)
    {
        _unitApi = unitApi;
        _eventBus = eventBus;
        _notification = notification;
        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = "إدارة الوحدات";
        this.Size = new Size(450, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;

        dgvUnits.DataSource = _bindingSource;
        dgvUnits.AutoGenerateColumns = false;
        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Symbol", HeaderText = "الرمز", Width = 80 });
        dgvUnits.ReadOnly = true;
        dgvUnits.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvUnits.MultiSelect = false;
        dgvUnits.AllowUserToAddRows = false;
        dgvUnits.RowHeadersVisible = false;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadUnitsAsync();
    }

    private async Task LoadUnitsAsync()
    {
        var result = await _unitApi.GetAllAsync();
        if (result.IsSuccess)
        {
            _bindingSource.DataSource = result.Value;
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private async void btnAdd_Click(object? sender, EventArgs e)
    {
        var name = txtNewName.Text.Trim();
        var symbol = txtNewSymbol.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            _notification.ShowWarning("يرجى إدخال اسم الوحدة");
            return;
        }

        var result = await _unitApi.CreateAsync(new CreateUnitRequest(name, symbol));
        if (result.IsSuccess)
        {
            txtNewName.Clear();
            txtNewSymbol.Clear();
            await LoadUnitsAsync();
            _eventBus.Publish(new UnitChangedMessage(result.Value.Id));
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private async void btnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvUnits.CurrentRow?.DataBoundItem is not UnitDto unit) return;

        if (MessageBox.Show($"هل أنت متأكد من حذف الوحدة '{unit.Name}'؟", "تأكيد الحذف", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var result = await _unitApi.UpdateAsync(unit.Id, new UpdateUnitRequest(unit.Name, unit.Symbol, false));
        if (result.IsSuccess)
        {
            await LoadUnitsAsync();
            _eventBus.Publish(new UnitChangedMessage(unit.Id));
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

        var addPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        txtNewName = new TextBox { Width = 150, PlaceholderText = "الاسم..." };
        txtNewSymbol = new TextBox { Width = 60, PlaceholderText = "الرمز..." };
        btnAdd = new Button { Text = "إضافة", Width = 70, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += btnAdd_Click;

        addPanel.Controls.Add(btnAdd);
        addPanel.Controls.Add(txtNewSymbol);
        addPanel.Controls.Add(txtNewName);

        dgvUnits = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnClose = new Button { Text = "إغلاق", Width = 80, FlatStyle = FlatStyle.Flat };
        btnClose.Click += (_, _) => this.Close();

        btnDelete = new Button { Text = "حذف", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.MistyRose };
        btnDelete.Click += btnDelete_Click;

        bottomPanel.Controls.Add(btnClose);
        bottomPanel.Controls.Add(btnDelete);

        mainLayout.Controls.Add(addPanel, 0, 0);
        mainLayout.Controls.Add(dgvUnits, 0, 1);
        mainLayout.Controls.Add(bottomPanel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}
