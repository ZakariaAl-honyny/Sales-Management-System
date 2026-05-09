using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Users;

public partial class UsersListControl : UserControl
{
    private readonly IUserApiService _apiService;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    
    private Button btnRefresh = null!;
    private DataGridView dgvUsers = null!;
    private Label lblStatusLabel = null!;

    public UsersListControl(IUserApiService apiService, INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        dgvUsers.DataSource = _bindingSource;
        dgvUsers.ReadOnly = true;
        dgvUsers.AllowUserToAddRows = false;
        dgvUsers.BackgroundColor = Color.White;
        dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var result = await _apiService.GetAllAsync();
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatusLabel.Text = $"ط¹ط¯ط¯ ط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ†: {result.Value.Count}";
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("ط®ط·ط£ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ†: " + ex.Message);
        }
    }

    private void FormatGrid()
    {
        if (dgvUsers.Columns.Count == 0) return;
        SetHeader("UserName", "ط§ط³ظ… ط§ظ„ظ…ط³طھط®ط¯ظ…");
        SetHeader("FullName", "ط§ظ„ط§ط³ظ… ط¨ط§ظ„ظƒط§ظ…ظ„");
        SetHeader("Role", "ط§ظ„طµظ„ط§ط­ظٹط©");
        SetHeader("IsActive", "ظ†ط´ط·");
    }

    private void SetHeader(string col, string text)
    {
        if (dgvUsers.Columns.Contains(col)) dgvUsers.Columns[col].HeaderText = text;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 80, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadUsersAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.Add(btnRefresh);
        topPanel.Controls.Add(flow);

        dgvUsers = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        lblStatusLabel = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Text = "ط¬ط§ظ‡ط²" };

        this.Controls.Add(dgvUsers);
        this.Controls.Add(lblStatusLabel);
        this.Controls.Add(topPanel);
    }
}
