    public class SuppliersListControl : UserControl
    {
        private readonly ISupplierApiService _supplierApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Button btnBalance = null!;
        private DataGridView dgvSuppliers = null!;
        private Label lblStatus = null!;

        public SuppliersListControl(ISupplierApiService supplierApiService)
        {
            _supplierApiService = supplierApiService;
            InitializeComponent();
            Load += async (_, _) => await LoadSuppliersAsync();
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
                PlaceholderText = "ابحث باسم المورد أو الكود أو الهاتف..."
            };

            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadSuppliersAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadSuppliersAsync();
            };

            btnAdd = new Button { Text = "إضافة", Width = 80 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync(null);

            btnEdit = new Button { Text = "تعديل", Width = 80 };
            btnEdit.Click += async (_, _) =>
            {
                var id = GetSelectedSupplierId();
                if (id is null)
                {
                    MessageBox.Show("اختر موردًا أولًا.");
                    return;
                }

                await OpenEditorAsync(id.Value);
            };

            btnDelete = new Button { Text = "حذف", Width = 80 };
            btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

            btnBalance = new Button { Text = "الرصيد", Width = 80 };
            btnBalance.Click += async (_, _) => await ShowBalanceAsync();

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 540,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            buttonsPanel.Controls.Add(btnBalance);
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

            dgvSuppliers = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };

            dgvSuppliers.DataSource = _bindingSource;
            dgvSuppliers.CellDoubleClick += async (_, _) =>
            {
                var id = GetSelectedSupplierId();
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

            Controls.Add(dgvSuppliers);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _supplierApiService.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد الموردين: {result.TotalCount}";

                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل الموردين";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvSuppliers.Columns.Count == 0)
                return;

            HideIfExists("Address");
            HideIfExists("OpeningBalance");
            HideIfExists("CreatedAt");
            HideIfExists("UpdatedAt");

            SetHeader("SupplierId", "ID");
            SetHeader("Code", "الكود");
            SetHeader("Name", "الاسم");
            SetHeader("Phone", "الهاتف");
            SetHeader("Email", "البريد");
            SetHeader("CurrentBalance", "الرصيد");
            SetHeader("IsActive", "نشط");
        }

        private void HideIfExists(string columnName)
        {
            if (dgvSuppliers.Columns.Contains(columnName))
                dgvSuppliers.Columns[columnName].Visible = false;
        }

        private void SetHeader(string columnName, string text)
        {
            if (dgvSuppliers.Columns.Contains(columnName))
                dgvSuppliers.Columns[columnName].HeaderText = text;
        }

        private int? GetSelectedSupplierId()
        {
            if (dgvSuppliers.CurrentRow?.DataBoundItem is not SupplierDto row)
                return null;

            return row.SupplierId;
        }

        private async Task OpenEditorAsync(int? supplierId)
        {
            using var form = new SupplierEditorForm(_supplierApiService, supplierId);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadSuppliersAsync();
        }

        private async Task DeleteSelectedAsync()
        {
            var id = GetSelectedSupplierId();
            if (id is null)
            {
                MessageBox.Show("اختر موردًا أولًا.");
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد حذف المورد؟",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                await _supplierApiService.DeleteAsync(id.Value);
                await LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ShowBalanceAsync()
        {
            var id = GetSelectedSupplierId();
            if (id is null)
            {
                MessageBox.Show("اختر موردًا أولًا.");
                return;
            }

            try
            {
                var balance = await _supplierApiService.GetBalanceAsync(id.Value);
                MessageBox.Show($"الرصيد الحالي: {balance:N2}", "رصيد المورد", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            btnBalance.Enabled = !busy;
        }
    }
}
```

---

## `SupplierEditorForm.cs`
```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Suppliers;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Suppliers
{
    public class SupplierEditorForm : Form
    {
        private readonly ISupplierApiService _supplierApiService;
        private readonly int? _supplierId;

        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtCode = null!;
        private TextBox txtName = null!;
        private TextBox txtPhone = null!;
        private TextBox txtEmail = null!;
        private TextBox txtAddress = null!;
        private NumericUpDown nudOpeningBalance = null!;
        private TextBox txtCurrentBalance = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public SupplierEditorForm(
            ISupplierApiService supplierApiService,
            int? supplierId = null)
        {
            _supplierApiService = supplierApiService;
            _supplierId = supplierId;

            InitializeComponent();
            Shown += async (_, _) => await LoadFormAsync();
        }

        private void InitializeComponent()
        {
            Text = _supplierId.HasValue ? "تعديل مورد" : "إضافة مورد";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 520);
            MinimumSize = new Size(650, 480);
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
                RowCount = 8
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            txtCode = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "الكود" };
            txtName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "اسم المورد" };
            txtPhone = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "الهاتف" };
            txtEmail = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "البريد الإلكتروني" };
            txtAddress = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "العنوان"
            };

            nudOpeningBalance = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            txtCurrentBalance = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
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

            mainPanel.Controls.Add(CreateLabeledPanel("الكود:", txtCode), 0, 0);
            mainPanel.Controls.Add(CreateLabeledPanel("اسم المورد:", txtName), 0, 1);
            mainPanel.Controls.Add(CreateLabeledPanel("الهاتف:", txtPhone), 0, 2);
            mainPanel.Controls.Add(CreateLabeledPanel("البريد الإلكتروني:", txtEmail), 0, 3);
            mainPanel.Controls.Add(CreateLabeledPanel("العنوان:", txtAddress), 0, 4);
            mainPanel.Controls.Add(CreateLabeledPanel("الرصيد الافتتاحي:", nudOpeningBalance), 0, 5);
            mainPanel.Controls.Add(CreateLabeledPanel("الرصيد الحالي:", txtCurrentBalance), 0, 6);
            mainPanel.Controls.Add(chkIsActive, 0, 7);
            mainPanel.Controls.Add(buttonsPanel, 0, 8);

            Controls.Add(mainPanel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            nudOpeningBalance.Enabled = !_supplierId.HasValue;
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
                if (!_supplierId.HasValue)
                {
                    txtCurrentBalance.Text = "0.00";
                    return;
                }

                UseWaitCursor = true;

                var supplier = await _supplierApiService.GetByIdAsync(_supplierId.Value);

                if (supplier is null)
                {
                    MessageBox.Show("المورد غير موجود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                txtCode.Text = supplier.Code ?? string.Empty;
                txtName.Text = supplier.Name;
                txtPhone.Text = supplier.Phone ?? string.Empty;
                txtEmail.Text = supplier.Email ?? string.Empty;
                txtAddress.Text = supplier.Address ?? string.Empty;
                chkIsActive.Checked = supplier.IsActive;
                nudOpeningBalance.Value = supplier.OpeningBalance;
                txtCurrentBalance.Text = supplier.CurrentBalance.ToString("N2");
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

                if (_supplierId.HasValue)
                {
                    var request = BuildUpdateRequest();
                    await _supplierApiService.UpdateAsync(_supplierId.Value, request);
                    MessageBox.Show("تم تحديث المورد بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var request = BuildCreateRequest();
                    await _supplierApiService.CreateAsync(request);
                    MessageBox.Show("تمت إضافة المورد بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _errorProvider.SetError(txtName, "اسم المورد مطلوب.");
                return false;
            }

            return true;
        }

        private CreateSupplierRequestDto BuildCreateRequest()
        {
            return new CreateSupplierRequestDto
            {
                Code = TrimToNull(txtCode.Text),
                Name = txtName.Text.Trim(),
                Phone = TrimToNull(txtPhone.Text),
                Email = TrimToNull(txtEmail.Text),
                Address = TrimToNull(txtAddress.Text),
                OpeningBalance = nudOpeningBalance.Value,
                IsActive = chkIsActive.Checked
            };
        }

        private UpdateSupplierRequestDto BuildUpdateRequest()
        {
            return new UpdateSupplierRequestDto
            {
                Code = TrimToNull(txtCode.Text),
                Name = txtName.Text.Trim(),
                Phone = TrimToNull(txtPhone.Text),
                Email = TrimToNull(txtEmail.Text),
                Address = TrimToNull(txtAddress.Text),
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

# 6) تسجيل Dependency Injection

## في `SalesSystem.Application/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ISupplierService, SupplierService>();
```

## في `SalesSystem.Infrastructure/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<ISupplierRepository, SupplierRepository>();
```

## في `SalesSystem.Desktop/Program.cs`
أضف:
```csharp
services.AddHttpClient<ISupplierApiService, SupplierApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<SuppliersListControl>();
services.AddTransient<SupplierEditorForm>();
