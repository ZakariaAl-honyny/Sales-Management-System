    public class PurchasesListControl : UserControl
    {
        private readonly IPurchaseApiService _purchaseApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnCancel = null!;
        private Button btnSummary = null!;
        private DataGridView dgvPurchases = null!;
        private Label lblStatus = null!;

        public PurchasesListControl(IPurchaseApiService purchaseApiService)
        {
            _purchaseApiService = purchaseApiService;
            InitializeComponent();
            Load += async (_, _) => await LoadPurchasesAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            txtSearch = new TextBox
            {
                Width = 260,
                PlaceholderText = "ابحث برقم الفاتورة أو اسم المورد..."
            };

            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadPurchasesAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadPurchasesAsync();
            };

            btnAdd = new Button { Text = "إضافة", Width = 80 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();

            btnCancel = new Button { Text = "إلغاء فاتورة", Width = 100 };
            btnCancel.Click += async (_, _) => await CancelSelectedAsync();

            btnSummary = new Button { Text = "ملخص", Width = 80 };
            btnSummary.Click += async (_, _) => await ShowSummaryAsync();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 470,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            buttonsPanel.Controls.Add(btnSummary);
            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnAdd);
            buttonsPanel.Controls.Add(btnRefresh);
            buttonsPanel.Controls.Add(btnSearch);

            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            searchPanel.Controls.Add(txtSearch);

            topPanel.Controls.Add(searchPanel);
            topPanel.Controls.Add(buttonsPanel);

            dgvPurchases = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };
            dgvPurchases.DataSource = _bindingSource;
            dgvPurchases.CellDoubleClick += async (_, _) =>
            {
                // لاحقاً يمكن فتح شاشة التفاصيل
                await Task.CompletedTask;
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "جاهز"
            };

            Controls.Add(dgvPurchases);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadPurchasesAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _purchaseApiService.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد الفواتير: {result.TotalCount}";

                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل فواتير الشراء";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvPurchases.Columns.Count == 0)
                return;

            HideIfExists("PurchaseInvoiceId");
            HideIfExists("CreatedByUserId");

            SetHeader("InvoiceNo", "رقم الفاتورة");
            SetHeader("SupplierName", "المورد");
            SetHeader("WarehouseName", "المخزن");
            SetHeader("InvoiceDate", "التاريخ");
            SetHeader("PaymentType", "الدفع");
            SetHeader("TotalAmount", "الإجمالي");
            SetHeader("PaidAmount", "المدفوع");
            SetHeader("DueAmount", "المتبقي");
            SetHeader("Status", "الحالة");
        }

        private void HideIfExists(string columnName)
        {
            if (dgvPurchases.Columns.Contains(columnName))
                dgvPurchases.Columns[columnName].Visible = false;
        }

        private void SetHeader(string columnName, string text)
        {
            if (dgvPurchases.Columns.Contains(columnName))
                dgvPurchases.Columns[columnName].HeaderText = text;
        }

        private int? GetSelectedPurchaseId()
        {
            if (dgvPurchases.CurrentRow?.DataBoundItem is not PurchaseInvoiceListDto row)
                return null;

            return row.PurchaseInvoiceId;
        }

        private async Task OpenEditorAsync()
        {
            using var form = new PurchaseEditorForm(
                FindForm()?.Tag as IServiceProvider ?? throw new InvalidOperationException("ServiceProvider not available"));

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadPurchasesAsync();
        }

        private async Task CancelSelectedAsync()
        {
            var id = GetSelectedPurchaseId();
            if (id is null)
            {
                MessageBox.Show("اختر فاتورة أولًا.");
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد إلغاء هذه الفاتورة؟",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                await _purchaseApiService.CancelAsync(id.Value);
                await LoadPurchasesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ShowSummaryAsync()
        {
            try
            {
                var summary = await _purchaseApiService.GetSummaryAsync(new PurchaseSummaryRequestDto
                {
                    From = DateTime.Today.AddDays(-30),
                    To = DateTime.Today
                });

                MessageBox.Show(
                    $"عدد الفواتير: {summary.TotalInvoices}\n" +
                    $"إجمالي الشراء: {summary.TotalPurchases:N2}\n" +
                    $"المدفوع: {summary.TotalPaid:N2}\n" +
                    $"المتبقي: {summary.TotalDue:N2}",
                    "ملخص الشراء",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetBusy(bool busy)
        {
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;

            txtSearch.Enabled = !busy;
            btnSearch.Enabled = !busy;
            btnRefresh.Enabled = !busy;
            btnAdd.Enabled = !busy;
            btnCancel.Enabled = !busy;
            btnSummary.Enabled = !busy;
        }
    }
}
```

> ملاحظة: `OpenEditorAsync` هنا يعتمد على `ServiceProvider` داخل `Form.Tag`.  
> إذا أردت، أرتب لك طريقة أفضل وموحدة للتنقل وحقن الخدمات في `MainForm` لاحقًا.

---

## `PurchaseEditorForm.cs`
هذا الفورم مخصص لإضافة فاتورة شراء جديدة مع عناصرها.

```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Invoices.Common;
using SalesSystem.Contracts.Products;
using SalesSystem.Contracts.Purchases;
using SalesSystem.Contracts.Suppliers;
using SalesSystem.Contracts.Warehouses;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Purchases
{
    public class PurchaseEditorForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPurchaseApiService _purchaseApiService;
        private readonly IProductApiService _productApiService;
        private readonly ISupplierApiService _supplierApiService;
        private readonly IWarehouseApiService _warehouseApiService;

        private readonly BindingList<PurchaseItemRowVm> _items = new();

        private ComboBox cmbSupplier = null!;
        private ComboBox cmbWarehouse = null!;
        private ComboBox cmbPaymentType = null!;
        private DateTimePicker dtInvoiceDate = null!;
        private DateTimePicker? dtDueDate = null!;
        private NumericUpDown nudDiscount = null!;
        private NumericUpDown nudTax = null!;
        private NumericUpDown nudPaid = null!;
        private TextBox txtNotes = null!;
        private ComboBox cmbProduct = null!;
        private NumericUpDown nudQty = null!;
        private NumericUpDown nudUnitCost = null!;
        private NumericUpDown nudItemDiscount = null!;
        private TextBox txtItemNotes = null!;
        private Button btnAddItem = null!;
        private Button btnRemoveItem = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private DataGridView dgvItems = null!;
        private Label lblSubTotal = null!;
        private Label lblTotal = null!;
        private Label lblDue = null!;

        private List<ProductLookupDto> _products = [];
        private List<SupplierLookupDto> _suppliers = [];
        private List<WarehouseLookupDto> _warehouses = [];

        public PurchaseEditorForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _purchaseApiService = serviceProvider.GetRequiredService<IPurchaseApiService>();
            _productApiService = serviceProvider.GetRequiredService<IProductApiService>();
            _supplierApiService = serviceProvider.GetRequiredService<ISupplierApiService>();
            _warehouseApiService = serviceProvider.GetRequiredService<IWarehouseApiService>();

            InitializeComponent();
            Shown += async (_, _) => await LoadLookupsAsync();
        }

        private void InitializeComponent()
        {
            Text = "إضافة فاتورة شراء";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = _items
            };

            var leftPanel = new Panel { Dock = DockStyle.Fill };

            var topForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = true
            };

            cmbSupplier = CreateCombo();
            cmbWarehouse = CreateCombo();
            cmbPaymentType = CreateCombo();
            cmbPaymentType.DataSource = Enum.GetValues(typeof(InvoicePaymentTypeDto));

            dtInvoiceDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            dtDueDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };

            nudDiscount = CreateMoney();
            nudTax = CreateMoney();
            nudPaid = CreateMoney();

            txtNotes = new TextBox { Width = 240, PlaceholderText = "ملاحظات الفاتورة" };

            topForm.Controls.Add(Wrap("المورد", cmbSupplier));
            topForm.Controls.Add(Wrap("المخزن", cmbWarehouse));
            topForm.Controls.Add(Wrap("نوع الدفع", cmbPaymentType));
            topForm.Controls.Add(Wrap("تاريخ الفاتورة", dtInvoiceDate));
            topForm.Controls.Add(Wrap("تاريخ الاستحقاق", dtDueDate));
            topForm.Controls.Add(Wrap("الخصم", nudDiscount));
            topForm.Controls.Add(Wrap("الضريبة", nudTax));
            topForm.Controls.Add(Wrap("المدفوع", nudPaid));
            topForm.Controls.Add(Wrap("ملاحظات", txtNotes));

            var itemPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = true
            };

            cmbProduct = CreateCombo();
            nudQty = CreateMoney(3);
            nudUnitCost = CreateMoney();
            nudItemDiscount = CreateMoney();
            txtItemNotes = new TextBox { Width = 180, PlaceholderText = "ملاحظات الصنف" };

            btnAddItem = new Button { Text = "إضافة الصنف", Width = 110, Height = 30 };
            btnAddItem.Click += (_, _) => AddItem();

            btnRemoveItem = new Button { Text = "حذف الصنف", Width = 110, Height = 30 };
            btnRemoveItem.Click += (_, _) => RemoveSelectedItem();

            cmbProduct.SelectedIndexChanged += (_, _) => FillUnitCostFromProduct();
            nudQty.ValueChanged += (_, _) => RecalcTotals();
            nudUnitCost.ValueChanged += (_, _) => RecalcTotals();
            nudItemDiscount.ValueChanged += (_, _) => RecalcTotals();
            nudDiscount.ValueChanged += (_, _) => RecalcTotals();
            nudTax.ValueChanged += (_, _) => RecalcTotals();
            nudPaid.ValueChanged += (_, _) => RecalcTotals();

            itemPanel.Controls.Add(Wrap("المنتج", cmbProduct));
            itemPanel.Controls.Add(Wrap("الكمية", nudQty));
            itemPanel.Controls.Add(Wrap("سعر الشراء", nudUnitCost));
            itemPanel.Controls.Add(Wrap("خصم الصنف", nudItemDiscount));
            itemPanel.Controls.Add(Wrap("ملاحظات", txtItemNotes));
            itemPanel.Controls.Add(btnAddItem);
            itemPanel.Controls.Add(btnRemoveItem);

            var details = new Panel { Dock = DockStyle.Fill };
            details.Controls.Add(itemPanel);
            details.Controls.Add(topForm);
            details.Controls.Add(dgvItems);

            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            lblSubTotal = new Label { AutoSize = true, Text = "الإجمالي الفرعي: 0.00" };
            lblTotal = new Label { AutoSize = true, Text = "الإجمالي: 0.00" };
            lblDue = new Label { AutoSize = true, Text = "المتبقي: 0.00" };

            btnSave = new Button { Text = "حفظ الفاتورة", Width = 130, Height = 35 };
            btnSave.Click += async (_, _) => await SaveAsync();

            btnCancel = new Button { Text = "إلغاء", Width = 100, Height = 35, DialogResult = DialogResult.Cancel };
            btnCancel.Click += (_, _) => Close();

            bottomPanel.Controls.Add(lblSubTotal);
            bottomPanel.Controls.Add(lblTotal);
            bottomPanel.Controls.Add(lblDue);
            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);

            root.Controls.Add(details, 0, 0);
            root.SetColumnSpan(details, 2);
            root.Controls.Add(bottomPanel, 0, 1);
            root.SetColumnSpan(bottomPanel, 2);

            Controls.Add(root);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private async Task LoadLookupsAsync()
        {
            var suppliersTask = _supplierApiService.GetLookupAsync();
            var warehousesTask = _warehouseApiService.GetLookupAsync();
            var productsTask = _productApiService.GetLookupAsync();

            await Task.WhenAll(suppliersTask, warehousesTask, productsTask);

            _suppliers = suppliersTask.Result.ToList();
            _warehouses = warehousesTask.Result.ToList();
            _products = productsTask.Result.ToList();

            cmbSupplier.DataSource = _suppliers;
            cmbSupplier.DisplayMember = nameof(SupplierLookupDto.Name);
            cmbSupplier.ValueMember = nameof(SupplierLookupDto.SupplierId);

            cmbWarehouse.DataSource = _warehouses;
            cmbWarehouse.DisplayMember = nameof(WarehouseLookupDto.Name);
            cmbWarehouse.ValueMember = nameof(WarehouseLookupDto.WarehouseId);

            cmbProduct.DataSource = _products;
            cmbProduct.DisplayMember = nameof(ProductLookupDto.Name);
            cmbProduct.ValueMember = nameof(ProductLookupDto.ProductId);

            if (_warehouses.Count > 0)
                cmbWarehouse.SelectedIndex = _warehouses.FindIndex(x => x.IsDefault) switch { -1 => 0, var idx => idx };

            if (_products.Count > 0)
                cmbProduct.SelectedIndex = 0;

            dtInvoiceDate.Value = DateTime.Now;
            RecalcTotals();
        }

        private void FillUnitCostFromProduct()
        {
            if (cmbProduct.SelectedItem is not ProductLookupDto product)
                return;

            nudUnitCost.Value = product.PurchasePrice;
        }

        private void AddItem()
        {
            if (cmbProduct.SelectedItem is not ProductLookupDto product)
            {
                MessageBox.Show("اختر منتجًا أولًا.");
                return;
            }

            if (nudQty.Value <= 0)
            {
                MessageBox.Show("الكمية يجب أن تكون أكبر من صفر.");
                return;
            }

            var row = new PurchaseItemRowVm
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Quantity = nudQty.Value,
                UnitCost = nudUnitCost.Value,
                DiscountAmount = nudItemDiscount.Value,
                Notes = TrimToNull(txtItemNotes.Text)
            };

            if (row.DiscountAmount > row.Quantity * row.UnitCost)
            {
                MessageBox.Show("خصم الصنف أكبر من قيمة الصنف.");
                return;
            }

            _items.Add(row);
            RecalcTotals();

            nudQty.Value = 1;
            nudItemDiscount.Value = 0;
            txtItemNotes.Clear();
            if (_products.Count > 0)
                cmbProduct.SelectedIndex = 0;
        }

        private void RemoveSelectedItem()
        {
            if (dgvItems.CurrentRow?.DataBoundItem is not PurchaseItemRowVm row)
            {
                MessageBox.Show("اختر صنفًا أولًا.");
                return;
            }

            _items.Remove(row);
            RecalcTotals();
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_items.Count == 0)
                {
                    MessageBox.Show("أضف صنفًا واحدًا على الأقل.");
                    return;
                }

                if (cmbSupplier.SelectedItem is not SupplierLookupDto supplier)
                {
                    MessageBox.Show("اختر المورد.");
                    return;
                }

                if (cmbWarehouse.SelectedItem is not WarehouseLookupDto warehouse)
                {
                    MessageBox.Show("اختر المخزن.");
                    return;
                }

                var total = CalculateTotal();
                var paymentType = (InvoicePaymentTypeDto)cmbPaymentType.SelectedItem!;

                if (paymentType == InvoicePaymentTypeDto.Cash)
                {
                    nudPaid.Value = total;
                }

                if (paymentType == InvoicePaymentTypeDto.Credit && nudPaid.Value != 0)
                {
                    MessageBox.Show("المدفوع يجب أن يكون صفرًا عند الشراء بالدين.");
                    return;
                }

                var request = new CreatePurchaseInvoiceRequestDto
                {
                    SupplierId = supplier.SupplierId,
