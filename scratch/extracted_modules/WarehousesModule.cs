    public class WarehousesListControl : UserControl
    {
        private readonly IWarehouseApiService _warehouseApiService;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Button btnSetDefault = null!;
        private DataGridView dgvWarehouses = null!;
        private Label lblStatus = null!;

        public WarehousesListControl(IWarehouseApiService warehouseApiService)
        {
            _warehouseApiService = warehouseApiService;
            InitializeComponent();
            Load += async (_, _) => await LoadWarehousesAsync();
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
                PlaceholderText = "ابحث باسم المخزن أو الكود..."
            };

            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadWarehousesAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) =>
            {
                txtSearch.Clear();
                await LoadWarehousesAsync();
            };

            btnAdd = new Button { Text = "إضافة", Width = 80 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync(null);

            btnEdit = new Button { Text = "تعديل", Width = 80 };
            btnEdit.Click += async (_, _) =>
            {
                var id = GetSelectedWarehouseId();
                if (id is null)
                {
                    MessageBox.Show("اختر مخزنًا أولًا.");
                    return;
                }

                await OpenEditorAsync(id.Value);
            };

            btnDelete = new Button { Text = "حذف", Width = 80 };
            btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

            btnSetDefault = new Button { Text = "افتراضي", Width = 80 };
            btnSetDefault.Click += async (_, _) =>
            {
                var id = GetSelectedWarehouseId();
                if (id is null)
                {
                    MessageBox.Show("اختر مخزنًا أولًا.");
                    return;
                }

                await SetDefaultAsync(id.Value);
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 460,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            buttonsPanel.Controls.Add(btnSetDefault);
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

            dgvWarehouses = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };
            dgvWarehouses.DataSource = _bindingSource;
            dgvWarehouses.CellDoubleClick += async (_, _) =>
            {
                var id = GetSelectedWarehouseId();
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

            Controls.Add(dgvWarehouses);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadWarehousesAsync()
        {
            try
            {
                SetBusy(true);

                var result = await _warehouseApiService.GetPagedAsync(
                    new PagedQueryRequestDto
                    {
                        PageNumber = 1,
                        PageSize = 200,
                        SearchTerm = txtSearch.Text
                    });

                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد المخازن: {result.TotalCount}";

                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "حدث خطأ أثناء تحميل المخازن";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void FormatGrid()
        {
            if (dgvWarehouses.Columns.Count == 0)
                return;

            if (dgvWarehouses.Columns.Contains("Location"))
                dgvWarehouses.Columns["Location"].HeaderText = "الموقع";

            if (dgvWarehouses.Columns.Contains("WarehouseId"))
                dgvWarehouses.Columns["WarehouseId"].HeaderText = "ID";

            if (dgvWarehouses.Columns.Contains("Code"))
                dgvWarehouses.Columns["Code"].HeaderText = "الكود";

            if (dgvWarehouses.Columns.Contains("Name"))
                dgvWarehouses.Columns["Name"].HeaderText = "الاسم";

            if (dgvWarehouses.Columns.Contains("IsDefault"))
                dgvWarehouses.Columns["IsDefault"].HeaderText = "افتراضي";

            if (dgvWarehouses.Columns.Contains("IsActive"))
                dgvWarehouses.Columns["IsActive"].HeaderText = "نشط";

            if (dgvWarehouses.Columns.Contains("CreatedAt"))
                dgvWarehouses.Columns["CreatedAt"].Visible = false;

            if (dgvWarehouses.Columns.Contains("UpdatedAt"))
                dgvWarehouses.Columns["UpdatedAt"].Visible = false;
        }

        private int? GetSelectedWarehouseId()
        {
            if (dgvWarehouses.CurrentRow?.DataBoundItem is not WarehouseDto row)
                return null;

            return row.WarehouseId;
        }

        private async Task OpenEditorAsync(int? warehouseId)
        {
            using var form = new WarehouseEditorForm(_warehouseApiService, warehouseId);

            var owner = FindForm();
            var result = owner is not null ? form.ShowDialog(owner) : form.ShowDialog();

            if (result == DialogResult.OK)
                await LoadWarehousesAsync();
        }

        private async Task DeleteSelectedAsync()
        {
            var id = GetSelectedWarehouseId();
            if (id is null)
            {
                MessageBox.Show("اختر مخزنًا أولًا.");
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد حذف المخزن؟",
                "تأكيد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                await _warehouseApiService.DeleteAsync(id.Value);
                await LoadWarehousesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SetDefaultAsync(int id)
        {
            try
            {
                await _warehouseApiService.SetDefaultAsync(id);
                await LoadWarehousesAsync();
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
            btnSetDefault.Enabled = !busy;
        }
    }
}
```

---

## `WarehouseEditorForm.cs`
```csharp
using System.ComponentModel;
using SalesSystem.Contracts.Warehouses;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Warehouses
{
    public class WarehouseEditorForm : Form
    {
        private readonly IWarehouseApiService _warehouseApiService;
        private readonly int? _warehouseId;

        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtCode = null!;
        private TextBox txtName = null!;
        private TextBox txtLocation = null!;
        private CheckBox chkIsDefault = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public WarehouseEditorForm(
            IWarehouseApiService warehouseApiService,
            int? warehouseId = null)
        {
            _warehouseApiService = warehouseApiService;
            _warehouseId = warehouseId;

            InitializeComponent();
            Shown += async (_, _) => await LoadFormAsync();
        }

        private void InitializeComponent()
        {
            Text = _warehouseId.HasValue ? "تعديل مخزن" : "إضافة مخزن";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(650, 380);
            MinimumSize = new Size(600, 340);
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
                RowCount = 5
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            txtCode = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "الكود"
            };

            txtName = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "اسم المخزن"
            };

            txtLocation = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "الموقع"
            };

            chkIsDefault = new CheckBox
            {
                Text = "افتراضي",
                Dock = DockStyle.Left
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
            mainPanel.Controls.Add(CreateLabeledPanel("اسم المخزن:", txtName), 0, 1);
            mainPanel.Controls.Add(CreateLabeledPanel("الموقع:", txtLocation), 0, 2);
            mainPanel.Controls.Add(CreateOptionsPanel(), 0, 3);
            mainPanel.Controls.Add(buttonsPanel, 0, 4);

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

        private Panel CreateOptionsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            panel.Controls.Add(chkIsActive);
            panel.Controls.Add(chkIsDefault);

            return panel;
        }

        private async Task LoadFormAsync()
        {
            try
            {
                if (!_warehouseId.HasValue)
                    return;

                UseWaitCursor = true;

                var warehouse = await _warehouseApiService.GetByIdAsync(_warehouseId.Value);

                if (warehouse is null)
                {
                    MessageBox.Show("المخزن غير موجود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                txtCode.Text = warehouse.Code ?? string.Empty;
                txtName.Text = warehouse.Name;
                txtLocation.Text = warehouse.Location ?? string.Empty;
                chkIsDefault.Checked = warehouse.IsDefault;
                chkIsActive.Checked = warehouse.IsActive;
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

                if (_warehouseId.HasValue)
                {
                    var request = BuildUpdateRequest();
                    await _warehouseApiService.UpdateAsync(_warehouseId.Value, request);
                    MessageBox.Show("تم تحديث المخزن بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var request = BuildCreateRequest();
                    await _warehouseApiService.CreateAsync(request);
                    MessageBox.Show("تمت إضافة المخزن بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _errorProvider.SetError(txtName, "اسم المخزن مطلوب.");
                return false;
            }

            return true;
        }

        private CreateWarehouseRequestDto BuildCreateRequest()
        {
            return new CreateWarehouseRequestDto
            {
                Code = TrimToNull(txtCode.Text),
                Name = txtName.Text.Trim(),
                Location = TrimToNull(txtLocation.Text),
                IsDefault = chkIsDefault.Checked,
                IsActive = chkIsActive.Checked
            };
        }

        private UpdateWarehouseRequestDto BuildUpdateRequest()
        {
            return new UpdateWarehouseRequestDto
            {
                Code = TrimToNull(txtCode.Text),
                Name = txtName.Text.Trim(),
                Location = TrimToNull(txtLocation.Text),
                IsDefault = chkIsDefault.Checked,
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

# 6) تسجيل الـ Dependency Injection

## في `SalesSystem.Application/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<IWarehouseService, WarehouseService>();
```

---

## في `SalesSystem.Infrastructure/DependencyInjection.cs`
أضف:
```csharp
services.AddScoped<IWarehouseRepository, WarehouseRepository>();
```

---

## في `SalesSystem.Desktop/Program.cs`
أضف:
```csharp
services.AddHttpClient<IWarehouseApiService, WarehouseApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<WarehousesListControl>();
services.AddTransient<WarehouseEditorForm>();
```

---

# 7) ما الذي يقدمه هذا الموديول؟
