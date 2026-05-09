using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Forms;

public partial class StockTransferForm : Form
{
    private readonly IStockTransferApiService _transferApi;
    private readonly IProductApiService _productApi;
    private readonly IWarehouseApiService _warehouseApi;
    private readonly INotificationService _notification;
    private readonly IEventBus _eventBus;
    
    private StockTransferDto? _transfer;
    private BindingList<TransferLineItemViewModel> _lines = new();
    private List<ProductDto> _allProducts = new();
    private bool _isUpdating = false;

    public StockTransferForm(
        IStockTransferApiService transferApi,
        IProductApiService productApi,
        IWarehouseApiService warehouseApi,
        INotificationService notification,
        IEventBus eventBus,
        StockTransferDto? transfer = null)
    {
        _transferApi = transferApi;
        _productApi = productApi;
        _warehouseApi = warehouseApi;
        _notification = notification;
        _eventBus = eventBus;
        _transfer = transfer;

        InitializeComponent();
        SetupGrid();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadLookups();

        if (_transfer != null) BindTransfer();
        else ResetForm();
        
        UpdateUIState();
    }

    private async Task LoadLookups()
    {
        var warehouseRes = await _warehouseApi.GetAllAsync();
        if (warehouseRes.IsSuccess)
        {
            var list1 = warehouseRes.Value!.ToList();
            var list2 = warehouseRes.Value!.ToList();
            
            cmbSource.DataSource = list1;
            cmbSource.DisplayMember = "Name";
            cmbSource.ValueMember = "Id";

            cmbDest.DataSource = list2;
            cmbDest.DisplayMember = "Name";
            cmbDest.ValueMember = "Id";

            if (_transfer == null && list1.Count > 1) cmbDest.SelectedIndex = 1;
        }

        var productRes = await _productApi.GetAllAsync();
        if (productRes.IsSuccess) _allProducts = productRes.Value!.ToList();
    }

    private void BindTransfer()
    {
        _isUpdating = true;
        this.Text = $"تحويل مخزني - {_transfer!.TransferNo}";
        lblTransferNo.Text = _transfer.TransferNo;
        dtpDate.Value = _transfer.TransferDate;
        cmbSource.SelectedValue = _transfer.FromWarehouseId;
        cmbDest.SelectedValue = _transfer.ToWarehouseId;
        txtNotes.Text = _transfer.Notes;

        _lines.Clear();
        foreach (var item in _transfer.Items)
        {
            _lines.Add(new TransferLineItemViewModel {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity
            });
        }
        _isUpdating = false;
    }

    private void ResetForm()
    {
        this.Text = "تحويل مخزني جديد";
        lblTransferNo.Text = "جديد";
        dtpDate.Value = DateTime.Now;
        _lines.Clear();
        txtNotes.Clear();
    }

    private void SetupGrid()
    {
        dgvItems.AutoGenerateColumns = false;
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "الكود/الباركود", DataPropertyName = "ProductCode", Width = 150 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "اسم المنتج", DataPropertyName = "ProductName", Width = 300, ReadOnly = true });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "الكمية", DataPropertyName = "Quantity", Width = 100 });
        dgvItems.DataSource = _lines;
        dgvItems.CellEndEdit += (s, e) => {
            if (_isUpdating || e.RowIndex < 0) return;
            var row = _lines[e.RowIndex];
            if (dgvItems.Columns[e.ColumnIndex].Name == "ColCode") {
                var p = _allProducts.FirstOrDefault(x => x.Code == row.ProductCode || x.Barcode == row.ProductCode);
                if (p != null) { row.ProductId = p.Id; row.ProductName = p.Name; if (row.Quantity == 0) row.Quantity = 1; }
            }
            dgvItems.Refresh();
        };
    }

    private void UpdateUIState()
    {
        bool editable = _transfer == null || _transfer.Status == (byte)InvoiceStatus.Draft;
        pnlHeader.Enabled = editable;
        dgvItems.ReadOnly = !editable;
        pnlFooter.Enabled = editable;
        btnAddItem.Enabled = editable;
        btnRemoveItem.Enabled = editable;
        btnSaveDraft.Visible = editable;
        btnPost.Visible = editable;
        lblStatus.Text = _transfer?.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "جديد" };
    }

    private void btnAddItem_Click(object sender, EventArgs e) { _lines.Add(new TransferLineItemViewModel()); dgvItems.Focus(); }
    private void btnRemoveItem_Click(object sender, EventArgs e) { if (dgvItems.CurrentRow?.DataBoundItem is TransferLineItemViewModel line) _lines.Remove(line); }

    private async void btnSaveDraft_Click(object sender, EventArgs e) => await SaveTransferAsync(false);
    private async void btnPost_Click(object sender, EventArgs e) => await SaveTransferAsync(true);

    private async Task SaveTransferAsync(bool post)
    {
        if (!_lines.Any(l => l.ProductId > 0)) { _notification.ShowWarning("يرجى إضافة أصناف"); return; }
        int srcId = (int)cmbSource.SelectedValue;
        int dstId = (int)cmbDest.SelectedValue;
        if (srcId == dstId) { _notification.ShowError("لا يمكن التحويل لنفس المستودع"); return; }
        if (post && MessageBox.Show("ترحيل التحويل؟ سيتم خصم الكميات من المصدر وإضافتها للوجهة.", "تأكيد", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        try {
            var items = _lines.Where(l => l.ProductId > 0).Select(l => new CreateStockTransferItemRequest(l.ProductId, l.Quantity, null)).ToList();
            var request = new CreateStockTransferRequest(srcId, dstId, dtpDate.Value, txtNotes.Text, items);
            
            var result = await _transferApi.CreateAsync(request);
            if (result.IsSuccess) {
                if (post) {
                    var postRes = await _transferApi.PostAsync(result.Value.Id);
                    if (postRes.IsSuccess) { _notification.ShowSuccess("تم الترحيل"); foreach (var item in items) _eventBus.Publish(new StockChangedMessage(item.ProductId)); }
                } else _notification.ShowSuccess("تم الحفظ");
                _eventBus.Publish(new StockTransferChangedMessage(result.Value.Id));
                this.DialogResult = DialogResult.OK; this.Close();
            } else _notification.ShowError(result.Error!);
        } catch (Exception ex) { _notification.ShowError("خطأ: " + ex.Message); }
    }

    private void btnClose_Click(object sender, EventArgs e) => this.Close();

    public class TransferLineItemViewModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
