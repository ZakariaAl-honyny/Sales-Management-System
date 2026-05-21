    public class ProductsListControl : UserControl
    {
        private readonly IProductApiService _productApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private DataGridView dgvProducts = null!;
        private Label lblStatus = null!;

        public ProductsListControl(IProductApiService productApiService)
        {
            _productApiService = productApiService;
            InitializeComponent();

            Load += async (_, _) => await LoadProductsAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(8)
            };

            txtSearch = new TextBox
            {
                Width = 260,
                PlaceholderText = "ابحث باسم المنتج أو الكود..."
            };

            btnSearch = new Button
            {
                Text = "بحث",
                Width = 80
            };
            btnSearch.Click += async (_, _) => await LoadProductsAsync();

            btnRefresh = new Button
            {
                Text = "تحديث",
                Width = 80,
                Left = 8
            };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadProductsAsync();
            };

            btnAdd = new Button
            {
                Text = "إضافة",
                Width = 80
            };
            btnAdd.Click += (_, _) =>
            {
                MessageBox.Show("سنضيف ProductEditorForm في الخطوة التالية.");
            };

            btnEdit = new Button
            {
                Text = "تعديل",
                Width = 80
            };
            btnEdit.Click += (_, _) =>
            {
                var id = GetSelectedProductId();
                if (id is null)
                {
                    MessageBox.Show("اختر منتجًا أولًا.");
                    return;
                }

                MessageBox.Show($"سنفتح شاشة تعديل المنتج رقم {id} في الخطوة التالية.");
            };

            btnDelete = new Button
            {
                Text = "حذف",
                Width = 80
            };
            btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 280,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            buttonsPanel.Controls.Add(btnDelete);
            buttonsPanel.Controls.Add(btnEdit);
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

            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };
            dgvProducts.DataSource = _bindingSource;
            dgvProducts.CellDoubleClick += async (_, _) =>
            {
                var id = GetSelectedProductId();
                if (id is null) return;

                MessageBox.Show($"سنفتح شاشة تعديل المنتج رقم {id} في الخطوة التالية.");
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "جاهز"
            };

            Controls.Add(dgvProducts);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _productApiService.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                var list = result.Items.ToList();
                _bindingSource.DataSource = list;

                lblStatus.Text = $"عدد المنتجات: {result.TotalCount}";
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل المنتجات";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvProducts.Columns.Count == 0)
                return;

            HideIfExists("Description");
            HideIfExists("CreatedAt");
            HideIfExists("UpdatedAt");

            SetHeader("ProductId", "ID");
            SetHeader("Code", "الكود");
            SetHeader("Barcode", "الباركود");
            SetHeader("Name", "الاسم");
            SetHeader("CategoryName", "التصنيف");
            SetHeader("UnitName", "الوحدة");
            SetHeader("PurchasePrice", "سعر الشراء");
            SetHeader("SalePrice", "سعر البيع");
            SetHeader("MinStock", "الحد الأدنى");
            SetHeader("IsActive", "نشط");
        }

        private void HideIfExists(string columnName)
        {
            if (dgvProducts.Columns.Contains(columnName))
                dgvProducts.Columns[columnName].Visible = false;
        }

        private void SetHeader(string columnName, string headerText)
        {
            if (dgvProducts.Columns.Contains(columnName))
                dgvProducts.Columns[columnName].HeaderText = headerText;
        }

        private int? GetSelectedProductId()
        {
            if (dgvProducts.CurrentRow?.DataBoundItem is not ProductDto row)
                return null;

            return row.ProductId;
        }

        private async Task DeleteSelectedAsync()
        {
            var id = GetSelectedProductId();
            if (id is null)
            {
                MessageBox.Show("اختر منتجًا أولًا.");
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد حذف المنتج؟",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                await _productApiService.DeleteAsync(id.Value);
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetBusy(bool busy)
        {
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;

            btnSearch.Enabled = !busy;
            btnRefresh.Enabled = !busy;
            btnAdd.Enabled = !busy;
            btnEdit.Enabled = !busy;
            btnDelete.Enabled = !busy;
            txtSearch.Enabled = !busy;
        }
    }
}
```

---

# 7) استخدام الـ Control داخل `MainForm`
إذا أردت عرض المنتجات داخل نافذة رئيسية:

## `MainForm.cs`
```csharp
using SalesSystem.Desktop.Controls.Products;

namespace SalesSystem.Desktop.Forms
{
    public class MainForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private Panel contentPanel = null!;

        public MainForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            LoadProductsScreen();
        }

        private void InitializeComponent()
        {
            Text = "Sales System";
            WindowState = FormWindowState.Maximized;

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(contentPanel);
        }

        private void LoadProductsScreen()
        {
            contentPanel.Controls.Clear();

            var control = _serviceProvider.GetRequiredService<ProductsListControl>();
            control.Dock = DockStyle.Fill;

            contentPanel.Controls.Add(control);
        }
    }
}
```

---

# 8) تسجيل `MainForm` و `ProductsListControl`
في `Program.cs` تأكد من تسجيلهما:

```csharp
services.AddTransient<ProductsListControl>();
services.AddTransient<MainForm>();
```

---

# 9) كيف يتم التدفق الآن؟
## عند فتح الشاشة:
- `ProductsListControl` ينادي `GET /api/products`

## عند الحذف:
- ينادي `DELETE /api/products/{id}`
- ثم يعمل `Refresh`

## عند الإضافة والتعديل:
- سنضيف `ProductEditorForm` في الخطوة التالية
- وبعد الحفظ:
  - يرجع إلى `ProductsListControl`
  - ثم يعمل `Reload`

---

# 10) ملاحظات مهمة جدًا
## أ) لماذا بدأنا بالمنتجات؟
لأنه أول CRUD بسيط، ويمثل نموذجًا لباقي الشاشات.

## ب) لماذا استخدمنا API Client داخل Desktop؟
لأنك اخترت:
- Desktop + API من البداية  
وهذا ممتاز مستقبلًا.

## ج) ما الخطوة التالية؟
الخطوة الطبيعية التالية هي:

### **ProductEditorForm**
حتى تقدر:
- تضيف منتج
- تعدل منتج
- ثم تعمل Refresh تلقائيًا

---

# 11) نصيحتي الآن
إذا أردت أن نكمل بشكل صحيح، فالخطوة التالية الأفضل هي:

## **بناء ProductEditorForm**
بشكل عملي، مع:
- Create
- Edit
- Validation بسيطة
- ثم Refresh للـ list

---

إذا أردت، سأكمل لك مباشرة في الرسالة القادمة بـ:

# **ProductEditorForm الكامل**
حتى يصبح عندك:
- إضافة
- تعديل
- حذف
- عرض
- وكلها متصلة بالـ API.

ممتاز.  
سأكمل لك الآن **ProductEditorForm** بشكل عملي، لكن قبل ذلك هناك **تعديل مهم جدًا** لازم نعمله في `ProductApiService` حتى لا تواجه خطأ عند `Update` و`Delete`.

---

# 1) تعديل مهم في `ProductApiService`
في النسخة السابقة، `UpdateAsync` و`DeleteAsync` كانا يحاولان قراءة `Data` حتى لو كانت `null`، وهذا قد يسبب مشكلة.

## استبدل `UpdateAsync` و`DeleteAsync` بهذا الشكل:

```csharp
public async Task UpdateAsync(
    int id,
    UpdateProductRequestDto request,
    CancellationToken cancellationToken = default)
{
    var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", request, cancellationToken);
    await EnsureSuccessAsync(response, cancellationToken);
}

public async Task DeleteAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    var response = await _httpClient.DeleteAsync($"api/products/{id}", cancellationToken);
    await EnsureSuccessAsync(response, cancellationToken);
}

private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
{
    if (response.IsSuccessStatusCode)
        return;

    var payload = await response.Content.ReadFromJsonAsync<ApiResponseDto<object?>>(cancellationToken: cancellationToken);

    throw new InvalidOperationException(
        payload?.Message ?? $"API Error: {response.StatusCode}");
}
```

---

# 2) ProductEditorForm
هذا هو الفورم الخاص بـ:
- إضافة منتج
- تعديل منتج
- الحفظ عبر الـ API

---

## `ProductEditorForm.cs`

```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Products;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Products
{
    public class ProductEditorForm : Form
    {
        private readonly IProductApiService _productApiService;
        private readonly int? _productId;

        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtCode = null!;
        private TextBox txtBarcode = null!;
        private TextBox txtName = null!;
        private TextBox txtDescription = null!;
        private NumericUpDown nudPurchasePrice = null!;
        private NumericUpDown nudSalePrice = null!;
        private NumericUpDown nudMinStock = null!;
        private CheckBox chkIsActive = null!;
        private CheckBox chkUseCategory = null!;
        private NumericUpDown nudCategoryId = null!;
        private CheckBox chkUseUnit = null!;
        private NumericUpDown nudUnitId = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Label lblTitle = null!;

        public ProductEditorForm(IProductApiService productApiService, int? productId = null)
        {
            _productApiService = productApiService;
            _productId = productId;

            InitializeComponent();

            Shown += async (_, _) => await LoadFormAsync();
        }

        private void InitializeComponent()
        {
            Text = _productId.HasValue ? "تعديل منتج" : "إضافة منتج";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(800, 650);
            MinimumSize = new Size(750, 600);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10),
                Text = _productId.HasValue ? "تعديل منتج" : "إضافة منتج جديد"
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 9,
                AutoSize = true
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Row 1: Code / Barcode
            txtCode = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "الكود" };
            txtBarcode = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "الباركود" };

            // Row 2: Name
            txtName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "اسم المنتج" };

            // Row 3: Category / Unit
            chkUseCategory = new CheckBox
            {
                Text = "تحديد التصنيف",
                Dock = DockStyle.Fill,
                Checked = false
            };
            chkUseCategory.CheckedChanged += (_, _) => nudCategoryId.Enabled = chkUseCategory.Checked;

            nudCategoryId = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 1000000,
                Enabled = false
            };

            chkUseUnit = new CheckBox
            {
                Text = "تحديد الوحدة",
                Dock = DockStyle.Fill,
                Checked = false
            };
            chkUseUnit.CheckedChanged += (_, _) => nudUnitId.Enabled = chkUseUnit.Checked;

            nudUnitId = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 1000000,
                Enabled = false
            };

            // Row 4: Prices
            nudPurchasePrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            nudSalePrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            // Row 5: Min stock
            nudMinStock = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 3,
                Minimum = 0,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            // Row 6: Description
            txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 100,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "الوصف"
            };

            // Row 7: Active
            chkIsActive = new CheckBox
            {
                Text = "نشط",
                Checked = true,
                Dock = DockStyle.Left
            };

            // Buttons
