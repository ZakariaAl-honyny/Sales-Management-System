    public class SupplierPaymentsListControl : UserControl
    {
        private readonly ISupplierPaymentApiService _service;
        private readonly ISupplierApiService _supplierApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnSummary = null!;
        private DataGridView dgvPayments = null!;
        private Label lblStatus = null!;

        public SupplierPaymentsListControl(
            ISupplierPaymentApiService service,
            ISupplierApiService supplierApiService)
        {
            _service = service;
            _supplierApiService = supplierApiService;
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
                PlaceholderText = "ابحث برقم السند أو اسم المورد..."
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

            HideIfExists("SupplierPaymentId");
            HideIfExists("CreatedByUserId");
            HideIfExists("PurchaseInvoiceId");

            SetHeader("PaymentNo", "رقم السند");
            SetHeader("SupplierName", "المورد");
            SetHeader("PurchaseInvoiceNo", "فاتورة الشراء");
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
            using var form = new SupplierPaymentEditorForm(_service, _supplierApiService, FindForm()?.Tag as IServiceProvider);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadPaymentsAsync();
        }

        private async Task ShowSummaryAsync()
        {
            try
            {
                var summary = await _service.GetSummaryAsync(new SupplierPaymentSummaryRequestDto
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

## `SupplierPaymentEditorForm.cs`
```csharp
using SalesSystem.Contracts.Purchases;
using SalesSystem.Contracts.SupplierPayments;
using SalesSystem.Contracts.Suppliers;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.SupplierPayments
{
    public class SupplierPaymentEditorForm : Form
    {
        private readonly ISupplierPaymentApiService _paymentService;
        private readonly ISupplierApiService _supplierApiService;
        private readonly IServiceProvider? _serviceProvider;

        private ComboBox cmbSupplier = null!;
        private ComboBox cmbInvoice = null!;
        private DateTimePicker dtPaymentDate = null!;
        private NumericUpDown nudAmount = null!;
        private ComboBox cmbPaymentMethod = null!;
        private TextBox txtReferenceNo = null!;
        private TextBox txtNotes = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private List<SupplierLookupDto> _suppliers = [];
        private List<PurchaseInvoiceListDto> _invoices = [];

        public SupplierPaymentEditorForm(
            ISupplierPaymentApiService paymentService,
            ISupplierApiService supplierApiService,
            IServiceProvider? serviceProvider = null)
        {
            _paymentService = paymentService;
            _supplierApiService = supplierApiService;
            _serviceProvider = serviceProvider;

            InitializeComponent();
            Shown += async (_, _) => await LoadLookupsAsync();
        }

        private void InitializeComponent()
        {
            Text = "إضافة سند دفع لمورد";
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

            cmbSupplier = CreateCombo();
            cmbInvoice = CreateCombo();
            dtPaymentDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };

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

            panel.Controls.Add(Wrap("المورد", cmbSupplier), 0, 0);
            panel.Controls.Add(Wrap("فاتورة الشراء (اختياري)", cmbInvoice), 0, 1);
            panel.Controls.Add(Wrap("تاريخ السند", dtPaymentDate), 0, 2);
            panel.Controls.Add(Wrap("المبلغ", nudAmount), 0, 3);
            panel.Controls.Add(Wrap("طريقة الدفع", cmbPaymentMethod), 0, 4);
            panel.Controls.Add(Wrap("الرقم المرجعي / الملاحظات", txtReferenceNo), 0, 5);
            panel.Controls.Add(buttons, 0, 6);

            Controls.Add(panel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            cmbSupplier.SelectedIndexChanged += async (_, _) => await LoadSupplierInvoicesAsync();
        }

        private async Task LoadLookupsAsync()
        {
            _suppliers = (await _supplierApiService.GetLookupAsync()).ToList();

            cmbSupplier.DataSource = _suppliers;
            cmbSupplier.DisplayMember = nameof(SupplierLookupDto.Name);
            cmbSupplier.ValueMember = nameof(SupplierLookupDto.SupplierId);

            if (_suppliers.Count > 0)
                cmbSupplier.SelectedIndex = 0;

            cmbPaymentMethod.SelectedIndex = 0;
            dtPaymentDate.Value = DateTime.Now;

            await LoadSupplierInvoicesAsync();
        }

        private async Task LoadSupplierInvoicesAsync()
        {
            if (cmbSupplier.SelectedItem is not SupplierLookupDto supplier)
            {
                cmbInvoice.DataSource = null;
                return;
            }

            if (_serviceProvider is null)
            {
                cmbInvoice.DataSource = null;
                return;
            }

            try
            {
                var purchaseApi = _serviceProvider.GetService(typeof(IPurchaseApiService)) as IPurchaseApiService;
                if (purchaseApi is null)
                {
                    cmbInvoice.DataSource = null;
                    return;
                }

                var invoices = await purchaseApi.GetPagedAsync(new SalesSystem.Contracts.Common.PagedQueryRequestDto
                {
                    PageNumber = 1,
                    PageSize = 200,
                    SearchTerm = supplier.Name
                });

                _invoices = invoices.Items.ToList();

                cmbInvoice.DataSource = _invoices;
                cmbInvoice.DisplayMember = nameof(PurchaseInvoiceListDto.InvoiceNo);
                cmbInvoice.ValueMember = nameof(PurchaseInvoiceListDto.PurchaseInvoiceId);
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
                if (cmbSupplier.SelectedItem is not SupplierLookupDto supplier)
                {
                    MessageBox.Show("اختر موردًا.");
                    return;
                }

                if (nudAmount.Value <= 0)
                {
                    MessageBox.Show("المبلغ يجب أن يكون أكبر من صفر.");
                    return;
                }

                var request = new CreateSupplierPaymentRequestDto
                {
                    SupplierId = supplier.SupplierId,
                    PurchaseInvoiceId = cmbInvoice.SelectedItem is PurchaseInvoiceListDto invoice
                        ? invoice.PurchaseInvoiceId
                        : null,
                    PaymentDate = dtPaymentDate.Value,
                    Amount = nudAmount.Value,
                    PaymentMethod = (byte)cmbPaymentMethod.SelectedValue!,
                    ReferenceNo = TrimToNull(txtReferenceNo.Text),
                    Notes = TrimToNull(txtNotes.Text)
                };

                var confirm = MessageBox.Show(
                    "هل تريد حفظ سند الدفع؟",
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

# 6) إضافة إلى `IInvoiceNumberGenerator`

أضف هذه الدالة:

```csharp
Task<string> GenerateSupplierPaymentNoAsync(CancellationToken cancellationToken = default);
```

---

## وفي `InvoiceNumberGenerator.cs`
أضف هذا:

```csharp
public async Task<string> GenerateSupplierPaymentNoAsync(
    CancellationToken cancellationToken = default)
{
    var prefix = $"SUPPAY-{DateTime.UtcNow:yyyyMMdd}-";

    var lastNo = await _context.SupplierPayments
        .AsNoTracking()
        .Where(x => x.PaymentNo.StartsWith(prefix))
        .OrderByDescending(x => x.PaymentNo)
        .Select(x => x.PaymentNo)
        .FirstOrDefaultAsync(cancellationToken);

    int next = 1;

    if (!string.IsNullOrWhiteSpace(lastNo))
    {
        var suffix = lastNo.Replace(prefix, "");
        if (int.TryParse(suffix, out var current))
            next = current + 1;
    }

    return $"{prefix}{next:D4}";
}
```

---

# 7) تسجيل Dependency Injection

## في `SalesSystem.Application/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
```

## في `SalesSystem.Infrastructure/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
```

## في `SalesSystem.Desktop/Program.cs`
أضف:
```csharp
services.AddHttpClient<ISupplierPaymentApiService, SupplierPaymentApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<SupplierPaymentsListControl>();
services.AddTransient<SupplierPaymentEditorForm>();
```

---

# 8) ماذا حققنا الآن؟
أصبح عندك موديول الموردين للمدفوعات جاهزًا:
- إضافة سند دفع
- ربطه بمورد
- ربطه بفاتورة شراء إن لزم
- تقليل رصيد المورد
- عرض قائمة السندات
- عرض ملخص المدفوعات

---

# 9) ملاحظات مهمة
- هذا الإصدار لا يحتوي على **إلغاء السند** أو **حذفه المحاسبي**، لأن هذا يحتاج منطقًا أدق.
- إذا أردت، أضيف لك لاحقًا:
  - **Cancel Supplier Payment**
  - **Edit Supplier Payment**
  - **Soft Delete**

---

إذا أردت، فالخطوة التالية الطبيعية هي:

## **Returns Module**
- Sales Return
- Purchase Return

وأستطيع أن أبنيه لك بنفس النمط الجاهز.

ممتاز، نكمل الآن بـ **Returns Module كامل**، وهو مهم جدًا لأنه يغطي:

- **مرتجع البيع**  
- **مرتجع الشراء**  
- تحديث المخزون
- تعديل أرصدة العملاء / الموردين
- حفظ السجلات
- إلغاء المرتجع عند الحاجة

سأبنيه لك على نفس النمط السابق.

---
