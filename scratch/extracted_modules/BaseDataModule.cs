    public class CategoriesListControl : UserControl
    {
        private readonly ICategoryApiService _categoryApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private DataGridView dgvCategories = null!;
        private Label lblStatus = null!;

        public CategoriesListControl(ICategoryApiService categoryApiService)
        {
            _categoryApiService = categoryApiService;
            InitializeComponent();
            Load += async (_, _) => await LoadCategoriesAsync();
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
                PlaceholderText = "ابحث باسم التصنيف..."
            };

            btnSearch = new Button
            {
                Text = "بحث",
                Width = 80
            };
            btnSearch.Click += async (_, _) => await LoadCategoriesAsync();

            btnRefresh = new Button
            {
                Text = "تحديث",
                Width = 80
            };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadCategoriesAsync();
            };

            btnAdd = new Button
            {
                Text = "إضافة",
                Width = 80
            };
            btnAdd.Click += async (_, _) => await OpenEditorAsync(null);

            btnEdit = new Button
            {
                Text = "تعديل",
                Width = 80
            };
            btnEdit.Click += async (_, _) =>
            {
                var id = GetSelectedCategoryId();
                if (id is null)
                {
                    MessageBox.Show("اختر تصنيفًا أولًا.");
                    return;
                }

                await OpenEditorAsync(id.Value);
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
                Width = 360,
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

            dgvCategories = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };
            dgvCategories.DataSource = _bindingSource;
            dgvCategories.CellDoubleClick += async (_, _) =>
            {
                var id = GetSelectedCategoryId();
                if (id is null) return;

                await OpenEditorAsync(id.Value);
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "جاهز"
            };

            Controls.Add(dgvCategories);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _categoryApiService.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد التصنيفات: {result.TotalCount}";

                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل التصنيفات";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvCategories.Columns.Count == 0)
                return;

            if (dgvCategories.Columns.Contains("Description"))
                dgvCategories.Columns["Description"].Visible = false;

            if (dgvCategories.Columns.Contains("CreatedAt"))
                dgvCategories.Columns["CreatedAt"].Visible = false;

            if (dgvCategories.Columns.Contains("UpdatedAt"))
                dgvCategories.Columns["UpdatedAt"].Visible = false;

            if (dgvCategories.Columns.Contains("CategoryId"))
                dgvCategories.Columns["CategoryId"].HeaderText = "ID";

            if (dgvCategories.Columns.Contains("Name"))
                dgvCategories.Columns["Name"].HeaderText = "الاسم";

            if (dgvCategories.Columns.Contains("IsActive"))
                dgvCategories.Columns["IsActive"].HeaderText = "نشط";
        }

        private int? GetSelectedCategoryId()
        {
            if (dgvCategories.CurrentRow?.DataBoundItem is not CategoryDto row)
                return null;

            return row.CategoryId;
        }

        private async Task OpenEditorAsync(int? categoryId)
        {
            using var form = new CategoryEditorForm(_categoryApiService, categoryId);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadCategoriesAsync();
        }

        private async Task DeleteSelectedAsync()
        {
            var id = GetSelectedCategoryId();
            if (id is null)
            {
                MessageBox.Show("اختر تصنيفًا أولًا.");
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد حذف التصنيف؟",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                await _categoryApiService.DeleteAsync(id.Value);
                await LoadCategoriesAsync();
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
            btnEdit.Enabled = !busy;
            btnDelete.Enabled = !busy;
        }
    }
}
```

---

# 7) نافذة إضافة/تعديل التصنيفات

ضعها في:

```text
SalesSystem.Desktop/Forms/Categories
```

---

## `CategoryEditorForm.cs`
```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Categories;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Categories
{
    public class CategoryEditorForm : Form
    {
        private readonly ICategoryApiService _categoryApiService;
        private readonly int? _categoryId;

        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtName = null!;
        private TextBox txtDescription = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public CategoryEditorForm(
            ICategoryApiService categoryApiService,
            int? categoryId = null)
        {
            _categoryApiService = categoryApiService;
            _categoryId = categoryId;

            InitializeComponent();
            Shown += async (_, _) => await LoadFormAsync();
        }

        private void InitializeComponent()
        {
            Text = _categoryId.HasValue ? "تعديل تصنيف" : "إضافة تصنيف";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(600, 350);
            MinimumSize = new Size(550, 320);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            txtName = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "اسم التصنيف"
            };

            txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "الوصف"
            };

            chkIsActive = new CheckBox
            {
                Text = "نشط",
                Checked = true,
                Dock = DockStyle.Left
            };

            btnSave = new Button
            {
                Text = "حفظ",
                Width = 120,
                Height = 35
            };
            btnSave.Click += async (_, _) => await SaveAsync();

            btnCancel = new Button
            {
                Text = "إلغاء",
                Width = 120,
                Height = 35,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (_, _) => Close();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            buttonsPanel.Controls.Add(btnSave);
            buttonsPanel.Controls.Add(btnCancel);

            mainPanel.Controls.Add(CreateLabeledPanel("اسم التصنيف:", txtName), 0, 0);
            mainPanel.Controls.Add(CreateLabeledPanel("الوصف:", txtDescription), 0, 1);
            mainPanel.Controls.Add(chkIsActive, 0, 2);
            mainPanel.Controls.Add(buttonsPanel, 0, 3);

            Controls.Add(mainPanel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private Panel CreateLabeledPanel(string labelText, Control control)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleRight
            };

            control.Dock = DockStyle.Fill;

            panel.Controls.Add(control);
            panel.Controls.Add(label);

            return panel;
        }

        private async Task LoadFormAsync()
        {
            try
            {
                if (!_categoryId.HasValue)
                    return;

                UseWaitCursor = true;

                var category = await _categoryApiService.GetByIdAsync(_categoryId.Value);

                if (category is null)
                {
                    MessageBox.Show("التصنيف غير موجود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                txtName.Text = category.Name;
                txtDescription.Text = category.Description ?? string.Empty;
                chkIsActive.Checked = category.IsActive;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!ValidateForm())
                return;

            try
            {
                UseWaitCursor = true;
                btnSave.Enabled = false;

                if (_categoryId.HasValue)
                {
                    var request = BuildUpdateRequest();
                    await _categoryApiService.UpdateAsync(_categoryId.Value, request);
                    MessageBox.Show("تم تحديث التصنيف بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var request = BuildCreateRequest();
                    await _categoryApiService.CreateAsync(request);
                    MessageBox.Show("تمت إضافة التصنيف بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                btnSave.Enabled = true;
            }
        }

        private bool ValidateForm()
        {
            _errorProvider.SetError(txtName, string.Empty);

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                _errorProvider.SetError(txtName, "اسم التصنيف مطلوب.");
                return false;
            }

            return true;
        }

        private CreateCategoryRequestDto BuildCreateRequest()
        {
            return new CreateCategoryRequestDto
            {
                Name = txtName.Text.Trim(),
                Description = TrimToNull(txtDescription.Text),
                IsActive = chkIsActive.Checked
            };
        }

        private UpdateCategoryRequestDto BuildUpdateRequest()
        {
            return new UpdateCategoryRequestDto
            {
                Name = txtName.Text.Trim(),
                Description = TrimToNull(txtDescription.Text),
                IsActive = chkIsActive.Checked
            };
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

# 8) تسجيل الـ Dependency Injection

## في `SalesSystem.Application/DependencyInjection.cs`
أضف:

```csharp
services.AddScoped<ICategoryService, CategoryService>();
```

---

## في `SalesSystem.Infrastructure/DependencyInjection.cs`
أضف:

```csharp
services.AddScoped<ICategoryRepository, CategoryRepository>();
```

---

## في `SalesSystem.Desktop/Program.cs`
أضف:

```csharp
services.AddHttpClient<ICategoryApiService, CategoryApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<CategoriesListControl>();
services.AddTransient<CategoryEditorForm>();
```

---

# 9) كيف ترتبط الشاشة بالمنتج؟
الآن بعد أن أصبح عندك Categories module:

- `ProductEditorForm` سيقرأ التصنيفات من:
  - `ICategoryApiService.GetLookupAsync()`
- وبالتالي الـ ComboBox سيعمل فعليًا
- وبعد إضافة/تعديل تصنيف:
  - يمكنك إعادة تحميل ComboBox عند فتح منتج جديد
  - أو لاحقًا نضيف Publisher-Subscriber لتحديثه تلقائيًا

---

# 10) ما الذي أنصح به بعد هذا؟
الخطوة المنطقية التالية هي:

## **Units Module كامل**
بنفس نفس النمط:
- DTOs
- Service
- Repository
- API Controller
- Desktop ListControl
- Desktop EditorForm

لأن الوحدات ستكمل شاشة المنتج أيضًا.

---

إذا أردت، أبدأ لك مباشرة في الرسالة القادمة بـ:

# **Units Module كامل**
بنفس الطريقة الجاهزة للنسخ والتطبيق.


الذي أنجزناه إلى الآن **كبير جدًا**، لكن ما زال هناك جزء مهم قبل أن نعتبر الـ MVP جاهزًا للتشغيل الكامل.

## تقديري الحالي
إذا اعتبرنا المشروع كاملًا = 100%  
