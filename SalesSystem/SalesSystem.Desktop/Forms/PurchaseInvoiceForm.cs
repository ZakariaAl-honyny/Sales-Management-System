using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Messaging.Messages;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class PurchaseInvoiceForm : Form
{
    private readonly IPurchaseInvoiceApiService _purchaseApi;
    private readonly ISupplierApiService _supplierApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    
    private PurchaseInvoiceDto? _invoice;
    private BindingList<InvoiceLineItemViewModel> _lines = new();
    private List<ProductDto> _allProducts = new();
    private bool _isUpdating = false;

    public PurchaseInvoiceForm(
        IPurchaseInvoiceApiService purchaseApi,
        ISupplierApiService supplierApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification,
        IEventBus eventBus,
        PurchaseInvoiceDto? invoice = null)
    {
        _purchaseApi = purchaseApi;
        _supplierApi = supplierApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;
        _eventBus = eventBus;
        _invoice = invoice;

        InitializeComponent();
        SetupGrid();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadLookups();

        if (_invoice != null) BindInvoice();
        else ResetForm();
        
        UpdateUIState();
        CalculateTotals();
    }

    private async Task LoadLookups()
    {
        var supplierRes = await _supplierApi.GetAllAsync();
        if (supplierRes.IsSuccess)
        {
            cmbSupplier.DataSource = supplierRes.Value;
            cmbSupplier.DisplayMember = "Name";
            cmbSupplier.ValueMember = "Id";
        }

        var warehouseRes = await _warehouseApi.GetAllAsync();
        if (warehouseRes.IsSuccess)
        {
            cmbWarehouse.DataSource = warehouseRes.Value;
            cmbWarehouse.DisplayMember = "Name";
            cmbWarehouse.ValueMember = "Id";
            if (_invoice == null)
            {
                var def = warehouseRes.Value!.FirstOrDefault(w => w.IsDefault);
                if (def != null) cmbWarehouse.SelectedValue = def.Id;
            }
        }

        var productRes = await _productApi.GetAllAsync();
        if (productRes.IsSuccess) _allProducts = productRes.Value!.ToList();
    }

    private void BindInvoice()
    {
        _isUpdating = true;
        this.Text = $"فاتورة مشتريات - {_invoice!.InvoiceNo}";
        lblInvoiceNo.Text = _invoice.InvoiceNo;
        dtpDate.Value = _invoice.InvoiceDate;
        cmbSupplier.SelectedValue = _invoice.SupplierId;
        cmbWarehouse.SelectedValue = _invoice.WarehouseId;
        cmbPaymentType.SelectedIndex = (byte)_invoice.PaymentType - 1;
        numInvoiceDiscount.Value = _invoice.DiscountAmount;
        numTaxRate.Value = 15;
        txtPaid.Text = _invoice.PaidAmount.ToString("N2");
        txtNotes.Text = _invoice.Notes;

        _lines.Clear();
        foreach (var item in _invoice.Items)
        {
            _lines.Add(new InvoiceLineItemViewModel {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                DiscountAmount = item.DiscountAmount
            });
        }
        _isUpdating = false;
    }

    private void ResetForm()
    {
        this.Text = "فاتورة مشتريات جديدة";
        lblInvoiceNo.Text = "جديد";
        dtpDate.Value = DateTime.Now;
        cmbPaymentType.SelectedIndex = 0;
        _lines.Clear();
        numInvoiceDiscount.Value = 0;
        txtPaid.Text = "0";
        txtNotes.Clear();
    }

    private void SetupGrid()
    {
        dgvItems.AutoGenerateColumns = false;
        dgvItems.Columns.Clear();
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "الكود/الباركود", DataPropertyName = "ProductCode", Width = 150 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "اسم المنتج", DataPropertyName = "ProductName", Width = 250, ReadOnly = true });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 80 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCost", HeaderText = "التكلفة", DataPropertyName = "UnitCost", Width = 100 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDisc", HeaderText = "خصم", DataPropertyName = "DiscountAmount", Width = 80 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "الإجمالي", DataPropertyName = "LineTotal", Width = 120, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", ForeColor = Color.Blue, Font = new Font(dgvItems.Font, FontStyle.Bold) } });

        dgvItems.DataSource = _lines;
        dgvItems.CellEndEdit += DgvItems_CellEndEdit;
    }

    private void DgvItems_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_isUpdating || e.RowIndex < 0) return;
        var row = _lines[e.RowIndex];
        var colName = dgvItems.Columns[e.ColumnIndex].Name;

        if (colName == "ColCode")
        {
            var product = _allProducts.FirstOrDefault(p => p.Code == row.ProductCode || p.Barcode == row.ProductCode);
            if (product != null)
            {
                row.ProductId = product.Id;
                row.ProductName = product.Name;
                row.UnitCost = product.PurchasePrice;
                if (row.Quantity == 0) row.Quantity = 1;
            }
        }
        CalculateTotals();
        dgvItems.Refresh();
    }

    private void CalculateTotals()
    {
        if (_isUpdating) return;
        decimal subTotal = _lines.Sum(x => x.LineTotal);
        decimal taxRate = numTaxRate.Value;
        bool isTaxInclusive = chkTaxInclusive.Checked;
        decimal invoiceDiscount = numInvoiceDiscount.Value;
        decimal paidAmount = txtPaid.DecimalValue;

        decimal taxAmount = isTaxInclusive ? subTotal - (subTotal / (1 + taxRate / 100m)) : subTotal * (taxRate / 100m);
        decimal totalAmount = subTotal - invoiceDiscount + (isTaxInclusive ? 0 : taxAmount);
        decimal dueAmount = totalAmount - paidAmount;

        lblSubTotalVal.Text = subTotal.ToString("N2");
        lblTaxAmountVal.Text = taxAmount.ToString("N2");
        lblTotalVal.Text = totalAmount.ToString("N2");
        lblDueVal.Text = dueAmount.ToString("N2");
        lblDueVal.ForeColor = dueAmount > 0 ? Color.Red : Color.Green;
    }

    private void UpdateUIState()
    {
        bool editable = _invoice == null || _invoice.Status == (byte)InvoiceStatus.Draft;
        pnlHeader.Enabled = editable;
        dgvItems.ReadOnly = !editable;
        pnlFooter.Enabled = editable;
        btnAddItem.Enabled = editable;
        btnRemoveItem.Enabled = editable;
        btnSaveDraft.Visible = editable;
        btnPost.Visible = editable;
        btnCancelInvoice.Visible = _invoice != null && _invoice.Status == (byte)InvoiceStatus.Posted;
        
        lblStatus.Text = _invoice?.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "جديد" };
        lblStatus.BackColor = _invoice?.Status switch { 1 => Color.Gray, 2 => Color.Green, 3 => Color.Red, _ => Color.Blue };
    }

    private void btnAddItem_Click(object sender, EventArgs e)
    {
        _lines.Add(new InvoiceLineItemViewModel());
        dgvItems.Focus();
        dgvItems.CurrentCell = dgvItems.Rows[_lines.Count - 1].Cells[0];
    }

    private void btnRemoveItem_Click(object sender, EventArgs e)
    {
        if (dgvItems.CurrentRow?.DataBoundItem is InvoiceLineItemViewModel line)
        {
            _lines.Remove(line);
            CalculateTotals();
        }
    }

    private async void btnSaveDraft_Click(object sender, EventArgs e) => await SaveInvoiceAsync(false);
    private async void btnPost_Click(object sender, EventArgs e) => await SaveInvoiceAsync(true);

    private async Task SaveInvoiceAsync(bool post)
    {
        if (!_lines.Any(l => l.ProductId > 0)) { _notification.ShowWarning("يرجى إضافة أصناف للفاتورة"); return; }
        if (post && MessageBox.Show("هل أنت متأكد من ترحيل فاتورة المشتريات؟ سيتم زيادة المخزون.", "تأكيد الترحيل", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        btnSaveDraft.Enabled = btnPost.Enabled = false;
        try
        {
            var items = _lines.Where(l => l.ProductId > 0).Select(l => new CreatePurchaseInvoiceItemRequest(l.ProductId, l.Quantity, l.UnitCost, l.DiscountAmount, null)).ToList();
            var request = new CreatePurchaseInvoiceRequest(
                (int)(cmbWarehouse.SelectedValue ?? 0),
                (int)(cmbSupplier.SelectedValue ?? 0),
                dtpDate.Value,
                null,
                (PaymentType)(cmbPaymentType.SelectedIndex + 1),
                numInvoiceDiscount.Value,
                decimal.Parse(lblTaxAmountVal.Text),
                txtPaid.DecimalValue,
                txtNotes.Text,
                items
            );

            var result = await _purchaseApi.CreateAsync(request);
            if (result.IsSuccess)
            {
                if (post)
                {
                    var postRes = await _purchaseApi.PostAsync(result.Value.Id);
                    if (postRes.IsSuccess)
                    {
                        _notification.ShowSuccess("تم الترحيل بنجاح");
                        foreach (var item in items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
                    }
                    else _notification.ShowError("تم الحفظ ولكن فشل الترحيل: " + postRes.Error);
                }
                else _notification.ShowSuccess("تم الحفظ بنجاح");

                _eventBus.Publish(new PurchaseInvoiceChangedMessage(result.Value.Id));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else _notification.ShowError(result.Error!);
        }
        catch (Exception ex) { _notification.ShowError("حدث خطأ: " + ex.Message); }
        finally { btnSaveDraft.Enabled = btnPost.Enabled = true; }
    }

    private async void btnCancelInvoice_Click(object sender, EventArgs e)
    {
        if (_invoice == null) return;
        if (MessageBox.Show("هل أنت متأكد من إلغاء الفاتورة؟ سيتم عكس المخزون.", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var result = await _purchaseApi.CancelAsync(_invoice.Id);
        if (result.IsSuccess)
        {
            _notification.ShowSuccess("تم الإلغاء بنجاح");
            _eventBus.Publish(new PurchaseInvoiceChangedMessage(_invoice.Id));
            foreach (var item in _invoice.Items) _eventBus.Publish(new StockChangedMessage(item.ProductId));
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else _notification.ShowError(result.Error!);
    }

    private void btnClose_Click(object sender, EventArgs e) => this.Close();

    public class InvoiceLineItemViewModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(LineTotal)); } }
        private decimal _unitCost;
        public decimal UnitCost { get => _unitCost; set { _unitCost = value; OnPropertyChanged(nameof(UnitCost)); OnPropertyChanged(nameof(LineTotal)); } }
        private decimal _discountAmount;
        public decimal DiscountAmount { get => _discountAmount; set { _discountAmount = value; OnPropertyChanged(nameof(DiscountAmount)); OnPropertyChanged(nameof(LineTotal)); } }
        public decimal LineTotal => (Quantity * UnitCost) - DiscountAmount;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
