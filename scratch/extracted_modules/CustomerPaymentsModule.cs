    public class CustomerPaymentsListControl : UserControl
    {
        private readonly ICustomerPaymentApiService _service;
        private readonly ICustomerApiService _customerApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnSummary = null!;
        private DataGridView dgvPayments = null!;
        private Label lblStatus = null!;

        public CustomerPaymentsListControl(
            ICustomerPaymentApiService service,
            ICustomerApiService customerApiService)
        {
            _service = service;
            _customerApiService = customerApiService;
            InitializeComponent();
            Load += async (_, _) => await LoadPaymentsAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            txtSearch = new TextBox
            {
                Width = 260,
                PlaceholderText = "ابحث برقم السند أو اسم العميل..."
            };

            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadPaymentsAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadPaymentsAsync();
            };

            btnAdd = new Button { Text = "إضافة", Width = 80 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();

            btnSummary = new Button { Text = "ملخص", Width = 80 };
            btnSummary.Click += async (_, _) => await ShowSummaryAsync();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 360,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            buttonsPanel.Controls.Add(btnSummary);
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

            dgvPayments = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };

            dgvPayments.DataSource = _bindingSource;

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "جاهز"
            };

            Controls.Add(dgvPayments);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadPaymentsAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _service.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد السندات: {result.TotalCount}";

                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل السندات";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvPayments.Columns.Count == 0)
                return;

            HideIfExists("CustomerPaymentId");
            HideIfExists("CreatedByUserId");
            HideIfExists("SalesInvoiceId");

            SetHeader("PaymentNo", "رقم السند");
            SetHeader("CustomerName", "العميل");
            SetHeader("SalesInvoiceNo", "فاتورة البيع");
            SetHeader("PaymentDate", "التاريخ");
            SetHeader("Amount", "المبلغ");
            SetHeader("PaymentMethod", "الطريقة");
        }

        private void HideIfExists(string columnName)
        {
            if (dgvPayments.Columns.Contains(columnName))
                dgvPayments.Columns[columnName].Visible = false;
        }

        private void SetHeader(string columnName, string text)
        {
            if (dgvPayments.Columns.Contains(columnName))
                dgvPayments.Columns[columnName].HeaderText = text;
        }

        private async Task OpenEditorAsync()
        {
            using var form = new CustomerPaymentEditorForm(_service, _customerApiService);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadPaymentsAsync();
        }

        private async Task ShowSummaryAsync()
        {
            try
            {
                var summary = await _service.GetSummaryAsync(new CustomerPaymentSummaryRequestDto
                {
                    From = DateTime.Today.AddDays(-30),
                    To = DateTime.Today
                });

                MessageBox.Show(
                    $"عدد السندات: {summary.TotalPayments}\n" +
                    $"إجمالي المدفوعات: {summary.TotalAmount:N2}",
                    "ملخص المدفوعات",
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
            btnSummary.Enabled = !busy;
        }
    }
}
```

---

## `CustomerPaymentEditorForm.cs`
```csharp
using SalesSystem.Contracts.CustomerPayments;
using SalesSystem.Contracts.Customers;
using SalesSystem.Contracts.Sales;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.CustomerPayments
{
    public class CustomerPaymentEditorForm : Form
    {
        private readonly ICustomerPaymentApiService _paymentService;
        private readonly ICustomerApiService _customerApiService;
        private readonly ISalesApiService? _salesApiService;

        private ComboBox cmbCustomer = null!;
        private ComboBox cmbInvoice = null!;
        private DateTimePicker dtPaymentDate = null!;
        private NumericUpDown nudAmount = null!;
        private ComboBox cmbPaymentMethod = null!;
        private TextBox txtReferenceNo = null!;
        private TextBox txtNotes = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private List<CustomerLookupDto> _customers = [];
        private List<SalesInvoiceListDto> _invoices = [];

        public CustomerPaymentEditorForm(
            ICustomerPaymentApiService paymentService,
            ICustomerApiService customerApiService,
            ISalesApiService? salesApiService = null)
        {
            _paymentService = paymentService;
            _customerApiService = customerApiService;
            _salesApiService = salesApiService;

            InitializeComponent();
            Shown += async (_, _) => await LoadLookupsAsync();
        }

        private void InitializeComponent()
        {
            Text = "إضافة سند قبض من عميل";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 450);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 7
            };

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            cmbCustomer = CreateCombo();
            cmbInvoice = CreateCombo();
            dtPaymentDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            nudAmount = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            cmbPaymentMethod = CreateCombo();
            cmbPaymentMethod.DataSource = new List<KeyValuePair<byte, string>>
            {
                new(1, "نقدي"),
                new(2, "تحويل بنكي"),
                new(3, "بطاقة"),
                new(4, "أخرى")
            };
            cmbPaymentMethod.DisplayMember = "Value";
            cmbPaymentMethod.ValueMember = "Key";

            txtReferenceNo = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "رقم مرجعي" };
            txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "ملاحظات" };

            btnSave = new Button { Text = "حفظ", Width = 120, Height = 35 };
            btnSave.Click += async (_, _) => await SaveAsync();

            btnCancel = new Button { Text = "إلغاء", Width = 120, Height = 35, DialogResult = DialogResult.Cancel };
            btnCancel.Click += (_, _) => Close();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);

            panel.Controls.Add(Wrap("العميل", cmbCustomer), 0, 0);
            panel.Controls.Add(Wrap("فاتورة البيع (اختياري)", cmbInvoice), 0, 1);
            panel.Controls.Add(Wrap("تاريخ السند", dtPaymentDate), 0, 2);
            panel.Controls.Add(Wrap("المبلغ", nudAmount), 0, 3);
            panel.Controls.Add(Wrap("طريقة الدفع", cmbPaymentMethod), 0, 4);
            panel.Controls.Add(Wrap("الرقم المرجعي / الملاحظات", txtReferenceNo), 0, 5);
            panel.Controls.Add(buttons, 0, 6);

            Controls.Add(panel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            cmbCustomer.SelectedIndexChanged += async (_, _) => await LoadCustomerInvoicesAsync();
        }

        private async Task LoadLookupsAsync()
        {
            _customers = (await _customerApiService.GetLookupAsync()).ToList();

            cmbCustomer.DataSource = _customers;
            cmbCustomer.DisplayMember = nameof(CustomerLookupDto.Name);
            cmbCustomer.ValueMember = nameof(CustomerLookupDto.CustomerId);

            if (_customers.Count > 0)
                cmbCustomer.SelectedIndex = 0;

            cmbPaymentMethod.SelectedIndex = 0;
            dtPaymentDate.Value = DateTime.Now;

            await LoadCustomerInvoicesAsync();
        }

        private async Task LoadCustomerInvoicesAsync()
        {
            if (_salesApiService is null || cmbCustomer.SelectedItem is not CustomerLookupDto customer)
            {
                cmbInvoice.DataSource = null;
                return;
            }

            try
            {
                var invoices = await _salesApiService.GetPagedAsync(new SalesSystem.Contracts.Common.PagedQueryRequestDto
                {
                    PageNumber = 1,
                    PageSize = 200,
                    SearchTerm = customer.Name
                });

                _invoices = invoices.Items.ToList();

                cmbInvoice.DataSource = _invoices;
                cmbInvoice.DisplayMember = nameof(SalesInvoiceListDto.InvoiceNo);
                cmbInvoice.ValueMember = nameof(SalesInvoiceListDto.SalesInvoiceId);
            }
            catch
            {
                cmbInvoice.DataSource = null;
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (cmbCustomer.SelectedItem is not CustomerLookupDto customer)
                {
                    MessageBox.Show("اختر عميلًا.");
                    return;
                }

                if (nudAmount.Value <= 0)
                {
                    MessageBox.Show("المبلغ يجب أن يكون أكبر من صفر.");
                    return;
                }

                var request = new CreateCustomerPaymentRequestDto
                {
                    CustomerId = customer.CustomerId,
                    SalesInvoiceId = cmbInvoice.SelectedItem is SalesInvoiceListDto invoice ? invoice.SalesInvoiceId : null,
                    PaymentDate = dtPaymentDate.Value,
                    Amount = nudAmount.Value,
                    PaymentMethod = (byte)cmbPaymentMethod.SelectedValue!,
                    ReferenceNo = TrimToNull(txtReferenceNo.Text),
                    Notes = TrimToNull(txtNotes.Text)
                };

                var confirm = MessageBox.Show(
                    "هل تريد حفظ سند القبض؟",
                    "تأكيد",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                    return;

                var id = await _paymentService.CreateAsync(request);
                MessageBox.Show($"تم حفظ السند بنجاح. رقم السجل: {id}", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ComboBox CreateCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static Panel Wrap(string labelText, Control control)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleRight
            };

            control.Dock = DockStyle.Fill;

            panel.Controls.Add(control);
            panel.Controls.Add(label);

            return panel;
        }

        private static string? TrimToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}
```

---

# 6) Dependency Injection

## في `SalesSystem.Application/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ICustomerPaymentService, CustomerPaymentService>();
```

## في `SalesSystem.Infrastructure/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ICustomerPaymentRepository, CustomerPaymentRepository>();
```

## في `SalesSystem.Desktop/Program.cs`
أضف:
```csharp
services.AddHttpClient<ICustomerPaymentApiService, CustomerPaymentApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<CustomerPaymentsListControl>();
services.AddTransient<CustomerPaymentEditorForm>();
```

---

# 7) ملاحظات مهمة جدًا

## أ) هذا الموديول الآن يحقق:
- تسجيل سند قبض
- ربطه بعميل
- ربطه بفاتورة بيع عند الحاجة
- إنقاص رصيد العميل
- عرض قائمة السندات
- عرض الملخص

## ب) ما الذي لم نضفْه بعد؟
- حذف أو إلغاء السند بشكل محاسبي صحيح
- تعديل سند موجود
- ربط السندات مع قيود أكثر تقدمًا

وهذا يمكن أن يأتي لاحقًا.

## ج) هل تحتاج SupplierPayments الآن؟
نعم، وهو بنفس النمط تقريبًا، ويمكن بناؤه مباشرة بعد هذا.

---

إذا أردت، أستطيع الآن أن أكمل لك مباشرة بـ:

# **SupplierPayments Module كامل**

أو إذا أردت الانتقال للمرتجعات، أستطيع بعدها مباشرة بناء:
- **Sales Returns**
- **Purchase Returns**


أكيد.  
نكمّل الآن بـ **Supplier Payments Module كامل**، وهو نفس فكرة `CustomerPayments` لكن للموردين.

هذا الموديول يسمح بـ:
- تسجيل دفعات للمورد
- ربط الدفعة بفاتورة شراء إن لزم
- تقليل رصيد المورد
- عرض قائمة الدفعات
- عرض ملخص الدفعات

---

# 1) Contracts

ضع الملفات هنا:

```text
SalesSystem.Contracts/SupplierPayments
```

---

## `SupplierPaymentDto.cs`
```csharp
namespace SalesSystem.Contracts.SupplierPayments
{
    public class SupplierPaymentDto
    {
        public int SupplierPaymentId { get; set; }
        public string PaymentNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public int? PurchaseInvoiceId { get; set; }
        public string? PurchaseInvoiceNo { get; set; }

        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public byte PaymentMethod { get; set; }

        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }

        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
```

---

## `SupplierPaymentListDto.cs`
```csharp
namespace SalesSystem.Contracts.SupplierPayments
{
    public class SupplierPaymentListDto
    {
        public int SupplierPaymentId { get; set; }
        public string PaymentNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? PurchaseInvoiceNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public byte PaymentMethod { get; set; }
    }
}
```

---

