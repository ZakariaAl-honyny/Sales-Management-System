using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Categories;

[System.ComponentModel.DesignerCategory("Code")]
public partial class CategoriesListControl : UserControl
{
    private readonly ICategoryApiService _categoryApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;
    private TextBox txtSearch = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private DataGridView dgvCategories = null!;
    private Label lblStatus = null!;

    public CategoriesListControl(
        ICategoryApiService categoryApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _categoryApi = categoryApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        dgvCategories.DataSource = _bindingSource;
        dgvCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCategories.MultiSelect = false;
        dgvCategories.ReadOnly = true;
        dgvCategories.AllowUserToAddRows = false;
        dgvCategories.BackgroundColor = Color.White;
        dgvCategories.BorderStyle = BorderStyle.None;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<CategoryChangedMessage>(async _ => await LoadCategoriesAsync());
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _categoryApi.GetAllAsync(false);
            
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatus.Text = $"عدد التصنيفات: {result.Value.Count}";
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في تحميل التصنيفات");
            _notification.ShowError("خطأ في تحميل التصنيفات. تم تسجيل التفاصيل للدعم الفني.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FormatGrid()
    {
        if (dgvCategories.Columns.Count == 0) return;
        var hides = new[] { "Description", "CreatedAt", "UpdatedAt", "CreatedByUserId" };
        foreach (var h in hides)
        {
            if (dgvCategories.Columns.Contains(h)) dgvCategories.Columns[h].Visible = false;
        }
        SetHeader("Id", "ID");
        SetHeader("Name", "الاسم");
        SetHeader("IsActive", "نشط");
    }

    private void SetHeader(string col, string text)
    {
        if (dgvCategories.Columns.Contains(col)) dgvCategories.Columns[col].HeaderText = text;
    }

    private void SetBusy(bool busy)
    {
        txtSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAdd.Enabled = !busy;
        btnEdit.Enabled = !busy;
        btnDelete.Enabled = !busy;
        dgvCategories.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
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

        btnAdd = new Button { Text = "إضافة تصنيف", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (_, _) => ShowEditor();

        btnEdit = new Button { Text = "تعديل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnEdit, ThemeHelper.ButtonType.Secondary);
        btnEdit.Click += (_, _) => {
            if (dgvCategories.CurrentRow?.DataBoundItem is CategoryDto c) ShowEditor(c);
        };

        btnDelete = new Button { Text = "حذف/تعطيل", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnDelete, ThemeHelper.ButtonType.Ghost);
        btnDelete.ForeColor = ThemeHelper.Danger;
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadCategoriesAsync(); };

        txtSearch = new TextBox { Width = 250, Margin = new Padding(8, 8, 8, 0) };
        ThemeHelper.ApplySearchBoxStyle(txtSearch);
        txtSearch.PlaceholderText = "ابحث باسم التصنيف...";
        txtSearch.TextChanged += async (_, _) => await LoadCategoriesAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh, txtSearch });
        topPanel.Controls.Add(toolbar);

        dgvCategories = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvCategories);
        
        lblStatus = new Label { 
            Dock = DockStyle.Fill, 
            TextAlign = ContentAlignment.MiddleLeft, 
            Padding = new Padding(10, 0, 10, 0),
            Text = "جاهز",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ThemeHelper.TextSecondary,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvCategories, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);
        
        this.Controls.Add(mainLayout);
    }

    private void ShowEditor(CategoryDto? c = null)
    {
        var dialog = _serviceProvider.GetRequiredService<CategoryManagerDialog>();
        // Note: CategoryManagerDialog seems to manage the list, but if we wanted a single editor:
        // For now, CategoryManagerDialog handles its own loading.
        dialog.ShowDialog();
    }

    private async Task DeleteSelectedAsync()
    {
        if (dgvCategories.CurrentRow?.DataBoundItem is not CategoryDto c) return;
        if (MessageBox.Show($"هل تريد حذف/تعطيل التصنيف '{c.Name}'؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var res = await _categoryApi.DeleteAsync(c.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess("تمت العملية بنجاح");
            _eventBus.Publish(new CategoryChangedMessage());
        }
        else _notification.ShowError(res.Error!);
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}



