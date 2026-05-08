using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Forms;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class ProductsControl : BaseModuleControl
{
    private readonly IProductApiService _productApi;
    private readonly ICategoryApiService _categoryApi;
    private readonly IUnitApiService _unitApi;
    private readonly INotificationService _notification;
    
    private DataGridView _grid;
    private SearchBarControl _searchBar;
    private ComboBox _cmbCategoryFilter;
    private LoadingOverlayControl _loadingOverlay;
    private BindingList<ProductDto> _productList = new();

    public ProductsControl(
        IProductApiService productApi,
        ICategoryApiService categoryApi,
        IUnitApiService unitApi,
        INotificationService notification)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
        _unitApi = unitApi;
        _notification = notification;
        
        InitializeComponent();
        SetupGrid();
    }

    private void InitializeComponent()
    {
        this.pnlTop = new System.Windows.Forms.Panel();
        this.btnNew = new System.Windows.Forms.Button();
        this._searchBar = new SearchBarControl();
        this._cmbCategoryFilter = new ComboBox();
        this._grid = new DataGridView();
        this._loadingOverlay = new LoadingOverlayControl();
        
        this.pnlTop.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
        this.SuspendLayout();

        this.pnlTop.Dock = DockStyle.Top;
        this.pnlTop.Height = 60;
        this.pnlTop.Padding = new Padding(10);
        this.pnlTop.Controls.AddRange(new Control[] { btnNew, _searchBar, _cmbCategoryFilter });

        this.btnNew.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.btnNew.FlatStyle = FlatStyle.Flat;
        this.btnNew.ForeColor = Color.White;
        this.btnNew.Location = new Point(10, 12);
        this.btnNew.Size = new Size(120, 35);
        this.btnNew.Text = "+ منتج جديد";
        this.btnNew.Click += (s, e) => ShowAddDialog();

        this._searchBar.Location = new Point(140, 12);
        this._searchBar.Placeholder = "بحث عن منتج...";
        this._searchBar.SearchChanged += async (s, text) => await RefreshData();

        this._cmbCategoryFilter.Location = new Point(450, 15);
        this._cmbCategoryFilter.Size = new Size(200, 30);
        this._cmbCategoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        this._cmbCategoryFilter.SelectedIndexChanged += async (s, e) => await RefreshData();

        this._grid.Dock = DockStyle.Fill;
        this._grid.BackgroundColor = Color.White;
        this._grid.BorderStyle = BorderStyle.None;
        this._grid.AllowUserToAddRows = false;
        this._grid.ReadOnly = true;
        this._grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this._grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ShowEditDialog(_productList[e.RowIndex]); };

        this.Controls.Add(_loadingOverlay);
        this.Controls.Add(_grid);
        this.Controls.Add(pnlTop);

        this.pnlTop.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        this.ResumeLayout(false);
    }

    private Panel pnlTop;
    private Button btnNew;

    private void SetupGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم المنتج", FillWeight = 2 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "التصنيف", FillWeight = 1 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitName", HeaderText = "الوحدة", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PurchasePrice", HeaderText = "الشراء", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SalePrice", HeaderText = "البيع", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "نشط", Width = 50 });
        
        foreach (DataGridViewColumn col in _grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadFilters();
        await RefreshData();
    }

    private async Task LoadFilters()
    {
        var result = await _categoryApi.GetAllAsync();
        if (result.IsSuccess)
        {
            var list = result.Value.ToList();
            list.Insert(0, new CategoryDto(0, "كل التصنيفات", null, true));
            _cmbCategoryFilter.DataSource = list;
            _cmbCategoryFilter.DisplayMember = "Name";
            _cmbCategoryFilter.ValueMember = "Id";
        }
    }

    private async Task RefreshData()
    {
        _loadingOverlay.ShowOverlay();
        int? categoryId = (int?)_cmbCategoryFilter.SelectedValue == 0 ? null : (int?)_cmbCategoryFilter.SelectedValue;
        var result = await _productApi.GetAllAsync(_searchBar.SearchText, categoryId);
        _loadingOverlay.HideOverlay();

        if (result.IsSuccess)
        {
            _productList = new BindingList<ProductDto>(result.Value.ToList());
            _grid.DataSource = _productList;
        }
        else _notification.ShowError(result.Error!);
    }

    private void ShowAddDialog()
    {
        using var diag = new ProductDialog(_productApi, _categoryApi, _unitApi, _notification);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    private void ShowEditDialog(ProductDto product)
    {
        using var diag = new ProductDialog(_productApi, _categoryApi, _unitApi, _notification, product);
        if (diag.ShowDialog() == DialogResult.OK) _ = RefreshData();
    }

    protected override void RegisterSubscriptions() { }
}
