using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class UsersControl : BaseModuleControl
{
    private readonly IUserApiService _apiService;
    private readonly INotificationService _notification;
    
    private DataGridView _grid;
    private BindingList<UserDto> _userList = new();

    public UsersControl(IUserApiService apiService, INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        InitializeComponent();
        SetupGrid();
    }

    private void InitializeComponent()
    {
        this._grid = new System.Windows.Forms.DataGridView();
        this.SuspendLayout();

        this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
        this._grid.BackgroundColor = System.Drawing.Color.White;
        this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this._grid.AllowUserToAddRows = false;
        this._grid.ReadOnly = true;
        this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

        this.Controls.Add(this._grid);
        this.RightToLeft = RightToLeft.Yes;
        this.Size = new System.Drawing.Size(1000, 700);
        this.ResumeLayout(false);
    }

    private void SetupGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UserName", HeaderText = "اسم المستخدم", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "الاسم الكامل", FillWeight = 2 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "الصلاحية", Width = 100 });
        
        foreach (DataGridViewColumn col in _grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        var result = await _apiService.GetAllAsync();
        if (result.IsSuccess)
        {
            _userList = new BindingList<UserDto>(result.Value.ToList());
            _grid.DataSource = _userList;
        }
        else _notification.ShowError(result.Error!);
    }

    protected override void RegisterSubscriptions() { }
}
