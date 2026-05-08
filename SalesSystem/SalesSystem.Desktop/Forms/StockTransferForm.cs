using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Inventory;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Forms;

public partial class StockTransferForm : Form
{
    private readonly IStockTransferApiService _transferApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly IProductApiService _productApi;
    private readonly INotificationService _notification;
    
    private BindingList<TransferItemViewModel> _lines = new();
    private List<ProductDto> _products = new();

    public StockTransferForm(
        IStockTransferApiService transferApi,
        IWarehouseApiService warehouseApi,
        IProductApiService productApi,
        INotificationService notification)
    {
        _transferApi = transferApi;
        _warehouseApi = warehouseApi;
        _productApi = productApi;
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
        var warehouses = await _warehouseApi.GetAllAsync();
        if (warehouses.IsSuccess)
        {
            var list = warehouses.Value.ToList();
            cmbFromWarehouse.DataSource = new BindingList<WarehouseDto>(list);
            cmbFromWarehouse.DisplayMember = "Name";
            cmbFromWarehouse.ValueMember = "Id";

            cmbToWarehouse.DataSource = new BindingList<WarehouseDto>(list.Select(x => x).ToList()); // Clone list for second combo
            cmbToWarehouse.DisplayMember = "Name";
            cmbToWarehouse.ValueMember = "Id";
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
            Width = 300,
            FlatStyle = FlatStyle.Flat
        };
        dgvItems.Columns.Add(colProduct);
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 100 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNotes", HeaderText = "ملاحظات", DataPropertyName = "Notes", Width = 200 });
        
        dgvItems.DataSource = _lines;
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        if (cmbFromWarehouse.SelectedValue == null || cmbToWarehouse.SelectedValue == null) { _notification.ShowWarning("يرجى اختيار المستودعات"); return; }
        if ((int)cmbFromWarehouse.SelectedValue == (int)cmbToWarehouse.SelectedValue) { _notification.ShowWarning("لا يمكن التحويل لنفس المستودع"); return; }
        if (_lines.Count == 0) { _notification.ShowWarning("يرجى إضافة أصناف للتحويل"); return; }

        var items = _lines.Select(x => new CreateStockTransferItemRequest(x.ProductId, x.Quantity, x.Notes)).ToList();
        var request = new CreateStockTransferRequest(
            (int)cmbFromWarehouse.SelectedValue,
            (int)cmbToWarehouse.SelectedValue,
            dtpDate.Value,
            txtNotes.Text,
            items
        );

        btnSave.Enabled = false;
        try
        {
            var result = await _transferApi.CreateAsync(request);
            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تمت عملية التحويل بنجاح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        finally { btnSave.Enabled = true; }
    }

    public class TransferItemViewModel
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
