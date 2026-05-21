    public class SalesListControl : UserControl
    {
        private readonly ISalesApiService _salesApiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnCancel = null!;
        private Button btnSummary = null!;
        private Button btnView = null!;
        private DataGridView dgvSales = null!;
        private Label lblStatus = null!;

        public SalesListControl(
            ISalesApiService salesApiService,
            IServiceProvider serviceProvider)
        {
            _salesApiService = salesApiService;
            _serviceProvider = serviceProvider;

            InitializeComponent();
            Load += async (_, _) => await LoadSalesAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            txtSearch = new TextBox
            {
                Width = 260,
                PlaceholderText = "ابحث برقم الفاتورة أو اسم العميل..."
            };

            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadSalesAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadSalesAsync();
            };

            btnAdd = new Button { Text = "إضافة", Width = 80 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();

            btnCancel = new Button { Text = "إلغاء فاتورة", Width = 100 };
            btnCancel.Click += async (_, _) => await CancelSelectedAsync();

            btnSummary = new Button { Text = "ملخص", Width = 80 };
            btnSummary.Click += async (_, _) => await ShowSummaryAsync();

            btnView = new Button { Text = "عرض", Width = 80 };
            btnView.Click += async (_, _) => await ShowSelectedDetailsAsync();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 560,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            buttonsPanel.Controls.Add(btnView);
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

            dgvSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };
            dgvSales.DataSource = _bindingSource;
            dgvSales.CellDoubleClick += async (_, _) => await ShowSelectedDetailsAsync();

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "جاهز"
            };

            Controls.Add(dgvSales);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadSalesAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _salesApiService.GetPagedAsync(
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
                lblStatus.Text = "حدث خطأ أثناء تحميل الفواتير";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvSales.Columns.Count == 0)
                return;

            HideIfExists("SalesInvoiceId");
            HideIfExists("CreatedByUserId");

            SetHeader("InvoiceNo", "رقم الفاتورة");
            SetHeader("CustomerName", "العميل");
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
            if (dgvSales.Columns.Contains(columnName))
                dgvSales.Columns[columnName].Visible = false;
        }

        private void SetHeader(string columnName, string text)
        {
            if (dgvSales.Columns.Contains(columnName))
                dgvSales.Columns[columnName].HeaderText = text;
        }

        private int? GetSelectedSaleId()
        {
            if (dgvSales.CurrentRow?.DataBoundItem is not SalesInvoiceListDto row)
                return null;

            return row.SalesInvoiceId;
        }

        private async Task OpenEditorAsync()
        {
            using var form = new SalesEditorForm(_serviceProvider);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadSalesAsync();
        }

        private async Task CancelSelectedAsync()
        {
            var id = GetSelectedSaleId();
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
                await _salesApiService.CancelAsync(id.Value);
                await LoadSalesAsync();
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
                var summary = await _salesApiService.GetSummaryAsync(new SalesSummaryRequestDto
                {
                    From = DateTime.Today.AddDays(-30),
                    To = DateTime.Today
                });

                MessageBox.Show(
                    $"عدد الفواتير: {summary.TotalInvoices}\n" +
                    $"إجمالي المبيعات: {summary.TotalSales:N2}\n" +
                    $"المدفوع: {summary.TotalPaid:N2}\n" +
                    $"المتبقي: {summary.TotalDue:N2}",
                    "ملخص المبيعات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ShowSelectedDetailsAsync()
        {
            var id = GetSelectedSaleId();
            if (id is null)
            {
                MessageBox.Show("اختر فاتورة أولًا.");
                return;
            }

            try
            {
                var invoice = await _salesApiService.GetByIdAsync(id.Value);
                if (invoice is null)
                {
                    MessageBox.Show("الفاتورة غير موجودة.");
                    return;
                }

                var text =
                    $"رقم الفاتورة: {invoice.InvoiceNo}\n" +
                    $"العميل: {invoice.CustomerName}\n" +
                    $"المخزن: {invoice.WarehouseName}\n" +
                    $"الإجمالي: {invoice.TotalAmount:N2}\n" +
                    $"المدفوع: {invoice.PaidAmount:N2}\n" +
                    $"المتبقي: {invoice.DueAmount:N2}\n" +
                    $"عدد الأصناف: {invoice.Items.Count}";

                MessageBox.Show(text, "تفاصيل الفاتورة", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            btnView.Enabled = !busy;
        }
    }
}
```

---

## `SalesEditorForm.cs`
```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Customers;
using SalesSystem.Contracts.Invoices.Common;
using SalesSystem.Contracts.Products;
using SalesSystem.Contracts.Sales;
using SalesSystem.Contracts.Warehouses;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Sales
{
    public class SalesEditorForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISalesApiService _salesApiService;
        private readonly IProductApiService _productApiService;
        private readonly ICustomerApiService _customerApiService;
        private readonly IWarehouseApiService _warehouseApiService;

        private readonly BindingList<SalesItemRowVm> _items = new();

        private ComboBox cmbCustomer = null!;
        private ComboBox cmbWarehouse = null!;
        private ComboBox cmbPaymentType = null!;
        private DateTimePicker dtInvoiceDate = null!;
        private DateTimePicker dtDueDate = null!;
        private NumericUpDown nudDiscount = null!;
        private NumericUpDown nudTax = null!;
        private NumericUpDown nudPaid = null!;
        private TextBox txtNotes = null!;
        private ComboBox cmbProduct = null!;
        private NumericUpDown nudQty = null!;
        private NumericUpDown nudUnitPrice = null!;
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
        private List<CustomerLookupDto> _customers = [];
        private List<WarehouseLookupDto> _warehouses = [];

        public SalesEditorForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _salesApiService = serviceProvider.GetRequiredService<ISalesApiService>();
            _productApiService = serviceProvider.GetRequiredService<IProductApiService>();
            _customerApiService = serviceProvider.GetRequiredService<ICustomerApiService>();
            _warehouseApiService = serviceProvider.GetRequiredService<IWarehouseApiService>();

            InitializeComponent();
            Shown += async (_, _) => await LoadLookupsAsync();
        }

        private void InitializeComponent()
        {
            Text = "إضافة فاتورة بيع";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            var headerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = true
            };

            cmbCustomer = CreateCombo();
            cmbWarehouse = CreateCombo();
            cmbPaymentType = CreateCombo();
            cmbPaymentType.DataSource = Enum.GetValues(typeof(InvoicePaymentTypeDto));

            dtInvoiceDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            dtDueDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };

            nudDiscount = CreateMoney();
            nudTax = CreateMoney();
            nudPaid = CreateMoney();

            txtNotes = new TextBox { Width = 240, PlaceholderText = "ملاحظات الفاتورة" };

            cmbPaymentType.SelectedIndexChanged += (_, _) => UpdatePaymentMode();
            nudDiscount.ValueChanged += (_, _) => RecalcTotals();
            nudTax.ValueChanged += (_, _) => RecalcTotals();
            nudPaid.ValueChanged += (_, _) => RecalcTotals();

            headerPanel.Controls.Add(Wrap("العميل", cmbCustomer));
            headerPanel.Controls.Add(Wrap("المخزن", cmbWarehouse));
            headerPanel.Controls.Add(Wrap("نوع الدفع", cmbPaymentType));
            headerPanel.Controls.Add(Wrap("تاريخ الفاتورة", dtInvoiceDate));
            headerPanel.Controls.Add(Wrap("تاريخ الاستحقاق", dtDueDate));
            headerPanel.Controls.Add(Wrap("الخصم", nudDiscount));
            headerPanel.Controls.Add(Wrap("الضريبة", nudTax));
            headerPanel.Controls.Add(Wrap("المدفوع", nudPaid));
            headerPanel.Controls.Add(Wrap("ملاحظات", txtNotes));

            var itemPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = true
            };

            cmbProduct = CreateCombo();
            nudQty = CreateMoney(3);
            nudUnitPrice = CreateMoney();
            nudItemDiscount = CreateMoney();
            txtItemNotes = new TextBox { Width = 180, PlaceholderText = "ملاحظات الصنف" };

            cmbProduct.SelectedIndexChanged += (_, _) => FillUnitPriceFromProduct();
            nudQty.ValueChanged += (_, _) => RecalcTotals();
            nudUnitPrice.ValueChanged += (_, _) => RecalcTotals();
            nudItemDiscount.ValueChanged += (_, _) => RecalcTotals();

            btnAddItem = new Button { Text = "إضافة الصنف", Width = 110, Height = 30 };
            btnAddItem.Click += (_, _) => AddItem();

            btnRemoveItem = new Button { Text = "حذف الصنف", Width = 110, Height = 30 };
            btnRemoveItem.Click += (_, _) => RemoveSelectedItem();

            itemPanel.Controls.Add(Wrap("المنتج", cmbProduct));
            itemPanel.Controls.Add(Wrap("الكمية", nudQty));
            itemPanel.Controls.Add(Wrap("سعر البيع", nudUnitPrice));
            itemPanel.Controls.Add(Wrap("خصم الصنف", nudItemDiscount));
            itemPanel.Controls.Add(Wrap("ملاحظات", txtItemNotes));
            itemPanel.Controls.Add(btnAddItem);
            itemPanel.Controls.Add(btnRemoveItem);

            dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = _items
            };

            var body = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(dgvItems);
            body.Controls.Add(itemPanel);
            body.Controls.Add(headerPanel);

            var footer = new FlowLayoutPanel
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

            footer.Controls.Add(lblSubTotal);
            footer.Controls.Add(lblTotal);
            footer.Controls.Add(lblDue);
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);

            root.Controls.Add(headerPanel, 0, 0);
            root.Controls.Add(dgvItems, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private async Task LoadLookupsAsync()
        {
            var productsTask = _productApiService.GetLookupAsync();
            var customersTask = _customerApiService.GetLookupAsync();
            var warehousesTask = _warehouseApiService.GetLookupAsync();

            await Task.WhenAll(productsTask, customersTask, warehousesTask);

            _products = productsTask.Result.ToList();
            _customers = customersTask.Result.ToList();
            _warehouses = warehousesTask.Result.ToList();

            cmbProduct.DataSource = _products;
            cmbProduct.DisplayMember = nameof(ProductLookupDto.Name);
            cmbProduct.ValueMember = nameof(ProductLookupDto.ProductId);

            cmbCustomer.DataSource = _customers;
            cmbCustomer.DisplayMember = nameof(CustomerLookupDto.Name);
            cmbCustomer.ValueMember = nameof(CustomerLookupDto.CustomerId);

            cmbWarehouse.DataSource = _warehouses;
            cmbWarehouse.DisplayMember = nameof(WarehouseLookupDto.Name);
            cmbWarehouse.ValueMember = nameof(WarehouseLookupDto.WarehouseId);

            if (_warehouses.Count > 0)
                cmbWarehouse.SelectedIndex = _warehouses.FindIndex(x => x.IsDefault) switch { -1 => 0, var idx => idx };

            if (_products.Count > 0)
                cmbProduct.SelectedIndex = 0;

            cmbPaymentType.SelectedItem = InvoicePaymentTypeDto.Cash;
            dtInvoiceDate.Value = DateTime.Now;
            UpdatePaymentMode();
            RecalcTotals();
        }

        private void UpdatePaymentMode()
        {
            var type = (InvoicePaymentTypeDto)cmbPaymentType.SelectedItem!;
            var isCash = type == InvoicePaymentTypeDto.Cash;

            dtDueDate.Enabled = !isCash;
            cmbCustomer.Enabled = type != InvoicePaymentTypeDto.Cash;

            if (isCash)
            {
                nudPaid.Enabled = false;
                nudPaid.Value = CalculateTotal();
                dtDueDate.Value = DateTime.Now;
            }
            else
            {
                nudPaid.Enabled = true;
            }

            RecalcTotals();
        }

        private void FillUnitPriceFromProduct()
        {
            if (cmbProduct.SelectedItem is not ProductLookupDto product)
                return;

            nudUnitPrice.Value = product.SalePrice;
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

            var row = new SalesItemRowVm
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Quantity = nudQty.Value,
                UnitPrice = nudUnitPrice.Value,
                DiscountAmount = nudItemDiscount.Value,
                Notes = TrimToNull(txtItemNotes.Text)
            };

            if (row.DiscountAmount > row.Quantity * row.UnitPrice)
            {
                MessageBox.Show("خصم الصنف أكبر من قيمة الصنف.");
                return;
            }

            _items.Add(row);
            RecalcTotals();

