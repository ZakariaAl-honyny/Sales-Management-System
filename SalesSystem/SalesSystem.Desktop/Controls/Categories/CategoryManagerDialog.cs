using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
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
        this.Size = new Size(400, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;

        dgvCategories.DataSource = _bindingSource;
        dgvCategories.AutoGenerateColumns = false;
        dgvCategories.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvCategories.ReadOnly = true;
        dgvCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCategories.MultiSelect = false;
        dgvCategories.AllowUserToAddRows = false;
        dgvCategories.RowHeadersVisible = false;
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
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

        var addPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        txtNewName = new TextBox { Width = 200, PlaceholderText = "اسم التصنيف الجديد..." };
        btnAdd = new Button { Text = "إضافة", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += btnAdd_Click;

        addPanel.Controls.Add(btnAdd);
        addPanel.Controls.Add(txtNewName);

        dgvCategories = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        btnClose = new Button { Text = "إغلاق", Width = 80, FlatStyle = FlatStyle.Flat };
        btnClose.Click += (_, _) => this.Close();

        btnDelete = new Button { Text = "حذف", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.MistyRose };
        btnDelete.Click += btnDelete_Click;

        bottomPanel.Controls.Add(btnClose);
        bottomPanel.Controls.Add(btnDelete);

        mainLayout.Controls.Add(addPanel, 0, 0);
        mainLayout.Controls.Add(dgvCategories, 0, 1);
        mainLayout.Controls.Add(bottomPanel, 0, 2);

        this.Controls.Add(mainLayout);
    }
}



