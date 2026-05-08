using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Sales;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;

namespace SalesSystem.Desktop.Forms;

public partial class SalesInvoiceForm : Form
{
    private readonly ISalesInvoiceApiService _salesApi;
    private readonly ICustomerApiService _customerApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    
    private SalesInvoiceDto? _invoice;
    private BindingList<InvoiceLineItemViewModel> _lines = new();
    private List<ProductDto> _products = new();

    public SalesInvoiceForm(
        ISalesInvoiceApiService salesApi,
        ICustomerApiService customerApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification,
        SalesInvoiceDto? invoice = null)
    {
        _salesApi = salesApi;
        _customerApi = customerApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;
        _invoice = invoice;

        InitializeComponent();
        SetupGrid();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadLookups();

        if (_invoice != null)
        {
            this.Text = $"فاتورة مبيعات - {_invoice.InvoiceNo}";
            cmbCustomer.SelectedValue = _invoice.CustomerId ?? 0;
            cmbWarehouse.SelectedValue = _invoice.WarehouseId;
            dtpDate.Value = _invoice.InvoiceDate;
            cmbPaymentType.SelectedIndex = _invoice.PaymentType - 1;
            txtDiscount.Text = _invoice.DiscountAmount.ToString("F2");
            txtTax.Text = _invoice.TaxAmount.ToString("F2");
            txtPaid.Text = _invoice.PaidAmount.ToString("F2");
            txtNotes.Text = _invoice.Notes;

            foreach (var item in _invoice.Items)
            {
                _lines.Add(new InvoiceLineItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount
                });
            }
            
            bool isDraft = _invoice.Status == 1;
            btnSave.Visible = isDraft;
            btnPost.Visible = isDraft;
            btnCancel.Visible = _invoice.Status == 2; // Can cancel posted
            
            pnlHeader.Enabled = isDraft;
            dgvItems.ReadOnly = !isDraft;
            pnlFooter.Enabled = isDraft;
        }
        else
        {
            this.Text = "فاتورة مبيعات جديدة";
            dtpDate.Value = DateTime.Now;
            cmbPaymentType.SelectedIndex = 0;
            btnCancel.Visible = false;
        }
        
        CalculateTotals();
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
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDiscount", HeaderText = "خصم", DataPropertyName = "DiscountAmount", Width = 80 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "الإجمالي", DataPropertyName = "LineTotal", Width = 100, ReadOnly = true });
        
        dgvItems.DataSource = _lines;
        
        dgvItems.CellValueChanged += (s, e) => {
            if (e.RowIndex >= 0)
            {
                var row = _lines[e.RowIndex];
                if (e.ColumnIndex == 0 && row.ProductId > 0)
                {
                    var p = _products.FirstOrDefault(x => x.Id == row.ProductId);
                    if (p != null)
                    {
                        row.UnitPrice = p.SalePrice;
                        row.Quantity = 1;
                    }
                }
                CalculateTotals();
                dgvItems.Refresh();
            }
        };
    }

    private void CalculateTotals()
    {
        decimal subtotal = _lines.Sum(x => x.LineTotal);
        decimal discount = txtDiscount.DecimalValue;
        decimal tax = txtTax.DecimalValue;
        decimal total = subtotal - discount + tax;
        decimal paid = txtPaid.DecimalValue;
        
        lblSubtotalValue.Text = subtotal.ToString("N2");
        lblTotalValue.Text = total.ToString("N2");
        lblDueValue.Text = (total - paid).ToString("N2");
    }

    private async void btnSave_Click(object sender, EventArgs e) => await SaveInvoice(false);
    private async void btnPost_Click(object sender, EventArgs e) => await SaveInvoice(true);

    private async Task SaveInvoice(bool post)
    {
        if (_lines.Count == 0) { _notification.ShowWarning("يرجى إضافة أصناف للفاتورة"); return; }
        
        var items = _lines.Select(x => new CreateSalesInvoiceItemRequest(x.ProductId, x.Quantity, x.UnitPrice, x.DiscountAmount, null)).ToList();
        var request = new CreateSalesInvoiceRequest(
            (int)cmbWarehouse.SelectedValue,
            (int)cmbCustomer.SelectedValue == 0 ? null : (int)cmbCustomer.SelectedValue,
            dtpDate.Value,
            null,
            (SalesSystem.Domain.Enums.PaymentType)(cmbPaymentType.SelectedIndex + 1),
            txtDiscount.DecimalValue,
            txtTax.DecimalValue,
            txtPaid.DecimalValue,
            txtNotes.Text,
            items
        );

        btnSave.Enabled = btnPost.Enabled = false;
        try
        {
            var result = await _salesApi.CreateAsync(request);
            if (result.IsSuccess)
            {
                if (post)
                {
                    var postResult = await _salesApi.PostAsync(result.Value.Id);
                    if (postResult.IsSuccess) _notification.ShowSuccess("تم حفظ وترحيل الفاتورة بنجاح");
                    else _notification.ShowError("تم الحفظ كمسودة ولكن فشل الترحيل: " + postResult.Error);
                }
                else _notification.ShowSuccess("تم حفظ المسودة بنجاح");
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        finally { btnSave.Enabled = btnPost.Enabled = true; }
    }

    private async void btnCancel_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("هل أنت متأكد من إلغاء هذه الفاتورة؟ لا يمكن التراجع عن هذه العملية.", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            var result = await _salesApi.CancelAsync(_invoice!.Id);
            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم إلغاء الفاتورة بنجاح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
    }

    public class InvoiceLineItemViewModel : INotifyPropertyChanged
    {
        private int _productId;
        public int ProductId { get => _productId; set { _productId = value; OnPropertyChanged(nameof(ProductId)); OnPropertyChanged(nameof(LineTotal)); } }
        public string ProductName { get; set; } = "";
        
        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(LineTotal)); } }
        
        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); OnPropertyChanged(nameof(LineTotal)); } }
        
        private decimal _discountAmount;
        public decimal DiscountAmount { get => _discountAmount; set { _discountAmount = value; OnPropertyChanged(nameof(DiscountAmount)); OnPropertyChanged(nameof(LineTotal)); } }
        
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
