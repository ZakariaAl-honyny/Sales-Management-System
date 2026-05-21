using Serilog;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;
using SalesSystem.Desktop.Messaging.Messages;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class PurchaseReturnForm : Form
{
    private readonly IPurchaseReturnApiService _returnApi;
    private readonly ISupplierApiService _supplierApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    
    private PurchaseReturnDto? _invoice;
    private BindingList<InvoiceLineItemViewModel> _lines = new();
    private List<ProductDto> _allProducts = new();
    private bool _isUpdating = false;

    public PurchaseReturnForm(
        IPurchaseReturnApiService returnApi,
        ISupplierApiService supplierApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification,
        IEventBus eventBus)
    {
        _returnApi = returnApi;
        _supplierApi = supplierApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;
        _eventBus = eventBus;

        InitializeComponent();
        SetupGrid();
    }

    public void LoadData(PurchaseReturnDto? invoice)
    {
        _invoice = invoice;
        if (_invoice != null) BindInvoice();
        else ResetForm();
        UpdateUIState();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadLookups();
        if (_invoice == null) ResetForm(); // Default state
    }

    private async Task LoadLookups()
    {
        var warehouseRes = await _warehouseApi.GetAllAsync();
        if (warehouseRes.IsSuccess)
        {
            cmbWarehouse.DataSource = warehouseRes.Value;
            cmbWarehouse.DisplayMember = "Name";
            cmbWarehouse.ValueMember = "Id";
        }

        var supplierRes = await _supplierApi.GetAllAsync();
        if (supplierRes.IsSuccess)
        {
            cmbSupplier.DataSource = supplierRes.Value;
            cmbSupplier.DisplayMember = "Name";
            cmbSupplier.ValueMember = "Id";
        }

        var productRes = await _productApi.GetAllAsync();
        if (productRes.IsSuccess) _allProducts = productRes.Value!.ToList();
    }

    private void BindInvoice()
    {
        _isUpdating = true;
        this.Text = $"مرتجع مشتريات - {_invoice!.ReturnNo}";
        lblInvoiceNo.Text = _invoice.ReturnNo;
        dtpDate.Value = _invoice.ReturnDate;
        cmbSupplier.SelectedValue = _invoice.SupplierId;
        cmbWarehouse.SelectedValue = _invoice.WarehouseId;
        txtNotes.Text = _invoice.Notes;

        _lines.Clear();
        foreach (var item in _invoice.Items)
        {
            _lines.Add(new InvoiceLineItemViewModel {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitCost,
                DiscountAmount = item.DiscountAmount
            });
        }
        _isUpdating = false;
        CalculateTotals();
    }

    private void ResetForm()
    {
        this.Text = "مرتجع مشتريات جديد";
        lblInvoiceNo.Text = "جديد";
        dtpDate.Value = DateTime.Now;
        _lines.Clear();
        txtNotes.Clear();
    }

    private void SetupGrid()
    {
        dgvItems.AutoGenerateColumns = false;
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "الكود/الباركود", DataPropertyName = "ProductCode", Width = 150 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "اسم المنتج", DataPropertyName = "ProductName", Width = 250, ReadOnly = true });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 80 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrice", HeaderText = "السعر", DataPropertyName = "UnitPrice", Width = 100 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "الإجمالي", DataPropertyName = "LineTotal", Width = 100, ReadOnly = true });
        dgvItems.DataSource = _lines;
        dgvItems.CellEndEdit += (s, e) => {
            if (_isUpdating || e.RowIndex < 0) return;
            var row = _lines[e.RowIndex];
            if (dgvItems.Columns[e.ColumnIndex].Name == "ColCode") {
                var p = _allProducts.FirstOrDefault(x => x.Code == row.ProductCode || x.Barcode == row.ProductCode);
                if (p != null) { row.ProductId = p.Id; row.ProductName = p.Name; row.UnitPrice = p.PurchasePrice; if (row.Quantity == 0) row.Quantity = 1; }
            }
            CalculateTotals();
            dgvItems.Refresh();
        };
    }

    private void CalculateTotals()
    {
        decimal total = _lines.Sum(x => x.LineTotal);
        lblTotalVal.Text = total.ToString("N2");
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
        lblStatus.Text = _invoice?.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "جديد" };
    }

    private void btnAddItem_Click(object sender, EventArgs e) { _lines.Add(new InvoiceLineItemViewModel()); dgvItems.Focus(); }
    private void btnRemoveItem_Click(object sender, EventArgs e) { if (dgvItems.CurrentRow?.DataBoundItem is InvoiceLineItemViewModel line) { _lines.Remove(line); CalculateTotals(); } }

    private async void btnSaveDraft_Click(object sender, EventArgs e) => await SaveInvoiceAsync(false);
    private async void btnPost_Click(object sender, EventArgs e) => await SaveInvoiceAsync(true);

    private async Task SaveInvoiceAsync(bool post)
    {
        if (!_lines.Any(l => l.ProductId > 0)) { _notification.ShowWarning("يرجى إضافة أصناف"); return; }
        if (post && MessageBox.Show("ترحيل المرتجع؟ سيتم تقليل المخزون وتقليل رصيد المورد.", "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        try {
            var items = _lines.Where(l => l.ProductId > 0).Select(l => new ReturnItemRequest(l.ProductId, l.Quantity, l.UnitPrice, l.DiscountAmount)).ToList();
            var request = new CreatePurchaseReturnRequest(
                null,
                null,
                (int)(cmbSupplier.SelectedValue ?? 0),
                (int)(cmbWarehouse.SelectedValue ?? 0),
                dtpDate.Value,
                txtNotes.Text,
                items);
            
            var result = await _returnApi.CreateAsync(request);
            if (result.IsSuccess) {
                if (post) {
                    var postRes = await _returnApi.PostAsync(result.Value.Id);
                    if (postRes.IsSuccess) { _notification.ShowSuccess("تم الترحيل"); foreach (var item in items) _eventBus.Publish(new StockChangedMessage(item.ProductId)); }
                    else _notification.ShowError(postRes.Error!);
                } else _notification.ShowSuccess("تم الحفظ");
                _eventBus.Publish(new PurchaseReturnChangedMessage(result.Value.Id));
                this.DialogResult = DialogResult.OK; this.Close();
            } else _notification.ShowError(result.Error!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "حدث خطأ في حفظ مرتجع المشتريات");
            _notification.ShowError("خطأ في حفظ المرتجع. تم تسجيل التفاصيل للدعم الفني.");
        }
    }

    private void btnClose_Click(object sender, EventArgs e) => this.Close();

    public class InvoiceLineItemViewModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
