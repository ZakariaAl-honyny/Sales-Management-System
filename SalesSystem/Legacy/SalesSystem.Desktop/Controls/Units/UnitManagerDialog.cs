using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Helpers;

namespace SalesSystem.Desktop.Controls.Units;

[System.ComponentModel.DesignerCategory("Code")]
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
        this.Size = new Size(550, 600);
        ThemeHelper.ApplyDialogStyle(this);

        dgvUnits.DataSource = _bindingSource;
        dgvUnits.AutoGenerateColumns = false;
        ThemeHelper.ApplyDataGridViewStyle(dgvUnits);
        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { 
            DataPropertyName = "Name", 
            HeaderText = "الاسم", 
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill 
        });
        dgvUnits.Columns.Add(new DataGridViewTextBoxColumn { 
            DataPropertyName = "Symbol", 
            HeaderText = "الرمز", 
            Width = 100 
        });
        
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Success);
        ThemeHelper.ApplyButtonStyle(btnDelete, ThemeHelper.ButtonType.Danger);
        ThemeHelper.ApplyButtonStyle(btnClose, ThemeHelper.ButtonType.Neutral);
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
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F)); // Add Row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Actions Row

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var addToolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        btnAdd = new Button { Text = "إضافة", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += btnAdd_Click;

        txtNewSymbol = new TextBox { Width = 100, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtNewSymbol);
        txtNewSymbol.PlaceholderText = "الرمز...";

        txtNewName = new TextBox { Width = 200, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtNewName);
        txtNewName.PlaceholderText = "اسم الوحدة الجديد...";

        addToolbar.Controls.AddRange(new Control[] { btnAdd, txtNewSymbol, txtNewName });
        topPanel.Controls.Add(addToolbar);

        dgvUnits = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvUnits);

        var bottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 249, 250) };
        var bottomToolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 12, 10, 0)
        };
        
        btnClose = new Button { Text = "إغلاق" };
        ThemeHelper.ApplyButtonStyle(btnClose, ThemeHelper.ButtonType.Secondary);
        btnClose.Click += (_, _) => this.Close();

        btnDelete = new Button { Text = "حذف المختار" };
        ThemeHelper.ApplyButtonStyle(btnDelete, ThemeHelper.ButtonType.Ghost);
        btnDelete.ForeColor = ThemeHelper.Danger;
        btnDelete.Click += btnDelete_Click;

        bottomToolbar.Controls.AddRange(new Control[] { btnClose, btnDelete });
        bottomPanel.Controls.Add(bottomToolbar);

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvUnits, 0, 1);
        mainLayout.Controls.Add(bottomPanel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}
