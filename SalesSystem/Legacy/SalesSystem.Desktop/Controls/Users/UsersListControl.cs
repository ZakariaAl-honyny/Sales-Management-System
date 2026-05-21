using SalesSystem.Contracts.Common;
using Serilog;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Users;

[System.ComponentModel.DesignerCategory("Code")]
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
                lblStatusLabel.Text = $"عدد المستخدمين: {result.Value.Count}";
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطأ في تحميل قائمة المستخدمين");
            _notification.ShowError("خطأ في تحميل المستخدمين. تم تسجيل التفاصيل للدعم الفني.");
        }
    }

    private void FormatGrid()
    {
        if (dgvUsers.Columns.Count == 0) return;
        SetHeader("UserName", "اسم المستخدم");
        SetHeader("FullName", "الاسم بالكامل");
        SetHeader("Role", "الصلاحية");
        SetHeader("IsActive", "نشط");
    }

    private void SetHeader(string col, string text)
    {
        if (dgvUsers.Columns.Contains(col)) dgvUsers.Columns[col].HeaderText = text;
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

        var btnAdd = new Button { Text = "مستخدم جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        // btnAdd.Click += (_, _) => ShowEditor();

        var btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        // btnEdit.Click += (_, _) => { if (dgvUsers.CurrentRow?.DataBoundItem is UserDto u) ShowEditor(u); };

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => await LoadUsersAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnRefresh });
        topPanel.Controls.Add(toolbar);

        dgvUsers = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvUsers);
        
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
        mainLayout.Controls.Add(dgvUsers, 0, 1);
        mainLayout.Controls.Add(lblStatusLabel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}
