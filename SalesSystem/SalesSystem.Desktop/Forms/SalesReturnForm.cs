using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Forms;

public partial class SalesReturnForm : Form
{
    private readonly ISalesReturnApiService _returnApi;
    private readonly ICustomerApiService _customerApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    
    private BindingList<ReturnLineItemViewModel> _lines = new();
    private List<ProductDto> _products = new();

    public SalesReturnForm(
        ISalesReturnApiService returnApi,
        ICustomerApiService customerApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification)
    {
        _returnApi = returnApi;
        _customerApi = customerApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;

        InitializeComponent();
        SetupGrid();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadLookups();
    }

    private async Task LoadLookups()
    {
        var customers = await _customerApi.GetAllAsync();
        if (customers.IsSuccess)
        {
            var list = customers.Value.ToList();
            list.Insert(0, new CustomerDto(0, null, "عميل نقدي", "", "", "", 0, 0, true));
            cmbCustomer.DataSource = list;
            cmbCustomer.DisplayMember = "Name";
            cmbCustomer.ValueMember = "Id";
        }

        var warehouses = await _warehouseApi.GetAllAsync();
        if (warehouses.IsSuccess)
        {
            cmbWarehouse.DataSource = warehouses.Value;
            cmbWarehouse.DisplayMember = "Name";
            cmbWarehouse.ValueMember = "Id";
        }

        var products = await _productApi.GetAllAsync();
        if (products.IsSuccess) _products = products.Value.ToList();
    }

    private void SetupGrid()
    {
        dgvItems.AutoGenerateColumns = false;
        var colProduct = new DataGridViewComboBoxColumn
        {
            Name = "ColProduct",
            HeaderText = "المنتج",
            DataSource = _products,
            DisplayMember = "Name",
            ValueMember = "Id",
            DataPropertyName = "ProductId",
            Width = 250,
            FlatStyle = FlatStyle.Flat
        };
        dgvItems.Columns.Add(colProduct);
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 80 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrice", HeaderText = "السعر", DataPropertyName = "UnitPrice", Width = 100 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "الإجمالي", DataPropertyName = "LineTotal", Width = 100, ReadOnly = true });
        
        dgvItems.DataSource = _lines;
        dgvItems.CellValueChanged += (s, e) => {
            if (e.RowIndex >= 0)
            {
                var row = _lines[e.RowIndex];
                if (e.ColumnIndex == 0 && row.ProductId > 0)
                {
                    var p = _products.FirstOrDefault(x => x.Id == row.ProductId);
                    if (p != null) row.UnitPrice = p.SalePrice;
                }
                CalculateTotal();
                dgvItems.Refresh();
            }
        };
    }

    private void CalculateTotal()
    {
        lblTotalValue.Text = _lines.Sum(x => x.LineTotal).ToString("N2");
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (_lines.Count == 0) { _notification.ShowWarning("يرجى إضافة أصناف للمرتجع"); return; }
        
        var items = _lines.Select(x => new ReturnItemRequest(x.ProductId, x.Quantity, x.UnitPrice, 0)).ToList();
        var request = new CreateSalesReturnRequest(
            null, // Optional original invoice
            (int)cmbCustomer.SelectedValue == 0 ? null : (int)cmbCustomer.SelectedValue,
            (int)cmbWarehouse.SelectedValue,
            dtpDate.Value,
            txtNotes.Text,
            items
        );

        btnSave.Enabled = false;
        try
        {
            var result = await _returnApi.CreateAsync(request);
            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم حفظ مرتجع المبيعات بنجاح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        finally { btnSave.Enabled = true; }
    }

    public class ReturnLineItemViewModel : INotifyPropertyChanged
    {
        private int _productId;
        public int ProductId { get => _productId; set { _productId = value; OnPropertyChanged(nameof(ProductId)); OnPropertyChanged(nameof(LineTotal)); } }
        
        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(LineTotal)); } }
        
        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); OnPropertyChanged(nameof(LineTotal)); } }
        
        public decimal LineTotal => Quantity * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
