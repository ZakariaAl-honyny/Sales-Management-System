using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Helpers;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Controls.Categories;

public partial class CategoryManagerDialog : Form
{
    private readonly ICategoryApiService _categoryApi;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private DataGridView dgvCategories = null!;
    private Button btnAdd = null!;
    private Button btnDelete = null!;
    private Button btnClose = null!;
    private TextBox txtNewName = null!;

    public CategoryManagerDialog(
        ICategoryApiService categoryApi,
        IEventBus eventBus,
        INotificationService notification)
    {
        _categoryApi = categoryApi;
        _eventBus = eventBus;
        _notification = notification;
        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = "إدارة التصنيفات";
        this.Size = new Size(500, 600);
        ThemeHelper.ApplyDialogStyle(this);

        dgvCategories.DataSource = _bindingSource;
        dgvCategories.AutoGenerateColumns = false;
        ThemeHelper.ApplyDataGridViewStyle(dgvCategories);
        dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { 
            DataPropertyName = "Name", 
            HeaderText = "الاسم", 
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill 
        });
        
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Success);
        ThemeHelper.ApplyButtonStyle(btnDelete, ThemeHelper.ButtonType.Danger);
        ThemeHelper.ApplyButtonStyle(btnClose, ThemeHelper.ButtonType.Neutral);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        var result = await _categoryApi.GetAllAsync();
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
        if (string.IsNullOrEmpty(name))
        {
            _notification.ShowWarning("يرجى إدخال اسم التصنيف");
            return;
        }

        var result = await _categoryApi.CreateAsync(new CreateCategoryRequest(name, ""));
        if (result.IsSuccess)
        {
            txtNewName.Clear();
            await LoadCategoriesAsync();
            _eventBus.Publish(new CategoryChangedMessage());
        }
        else
        {
            _notification.ShowError(result.Error!);
        }
    }

    private async void btnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvCategories.CurrentRow?.DataBoundItem is not CategoryDto category) return;
        if (MessageBox.Show($"هل أنت متأكد من حذف التصنيف '{category.Name}'؟", "تأكيد الحذف", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        
        var result = await _categoryApi.UpdateAsync(category.Id, new UpdateCategoryRequest(category.Name, category.Description, false));
        if (result.IsSuccess)
        {
            await LoadCategoriesAsync();
            _eventBus.Publish(new CategoryChangedMessage());
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

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F)); // Add Category Row
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

        txtNewName = new TextBox { Width = 250, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtNewName);
        txtNewName.PlaceholderText = "أدخل اسم التصنيف الجديد...";

        addToolbar.Controls.AddRange(new Control[] { btnAdd, txtNewName });
        topPanel.Controls.Add(addToolbar);

        dgvCategories = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvCategories);

        var bottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 249, 250), Margin = new Padding(0) };
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
        mainLayout.Controls.Add(dgvCategories, 0, 1);
        mainLayout.Controls.Add(bottomPanel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}



