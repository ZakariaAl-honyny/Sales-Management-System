using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Categories;

public partial class CategoriesListControl : UserControl
{
    private readonly ICategoryApiService _categoryApi;
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
            _notification.ShowError("حدث خطأ أثناء تحميل البيانات: " + ex.Message);
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
        btnSearch.Enabled = !busy;
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
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        txtSearch = new TextBox { Width = 250, PlaceholderText = "ابحث باسم التصنيف..." };
        btnSearch = new Button { Text = "بحث", Width = 80, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadCategoriesAsync();
        
        btnRefresh = new Button { Text = "تحديث", Width = 80, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadCategoriesAsync(); };
        
        btnAdd = new Button { Text = "إضافة", Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += (_, _) => ShowEditor();
        
        btnEdit = new Button { Text = "تعديل", Width = 80, FlatStyle = FlatStyle.Flat };
        btnEdit.Click += (_, _) => {
            if (dgvCategories.CurrentRow?.DataBoundItem is CategoryDto c) ShowEditor(c);
        };
        
        btnDelete = new Button { Text = "حذف/تعطيل", Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.MistyRose };
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnDelete, btnEdit, btnAdd, btnRefresh, btnSearch, txtSearch });
        topPanel.Controls.Add(flow);

        dgvCategories = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        lblStatus = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

        this.Controls.Add(dgvCategories);
        this.Controls.Add(lblStatus);
        this.Controls.Add(topPanel);
    }

    private void ShowEditor(CategoryDto? c = null)
    {
        var factory = _serviceProvider.GetRequiredService<CategoryDialogFactory>();
        var d = factory.Create(c);
        if (d.ShowDialog() == DialogResult.OK) _eventBus.Publish(new CategoryChangedMessage());
    }

    private async Task DeleteSelectedAsync()
    {
        if (dgvCategories.CurrentRow?.DataBoundItem is not CategoryDto c) return;
        var msg = "هل تريد حذف/تعطيل هذا التصنيف؟";
        if (MessageBox.Show(msg, "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var res = await _categoryApi.DeleteAsync(c.Id);
        if (res.IsSuccess)
        {
            _notification.ShowSuccess("تمت العملية بنجاح");
            _eventBus.Publish(new CategoryChangedMessage());
        }
        else
        {
            _notification.ShowError(res.Error!);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}

public class CategoryDialogFactory
{
    private readonly IServiceProvider _sp;
    public CategoryDialogFactory(IServiceProvider sp) => _sp = sp;
    public CategoryDialog Create(CategoryDto? c = null) => 
        ActivatorUtilities.CreateInstance<CategoryDialog>(_sp, c ?? (object)Type.Missing);
}

