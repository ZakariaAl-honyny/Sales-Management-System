    public class SalesReturnsListControl : UserControl
    {
        private readonly ISalesReturnApiService _returnApiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnRefresh = null!;
        private Button btnAdd = null!;
        private DataGridView dgvReturns = null!;
        private Label lblStatus = null!;

        public SalesReturnsListControl(ISalesReturnApiService returnApiService, IServiceProvider serviceProvider)
        {
            _returnApiService = returnApiService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            Load += async (_, _) => await LoadReturnsAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            txtSearch = new TextBox { Width = 260, PlaceholderText = "ابحث برقم المرتجع أو العميل..." };
            btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadReturnsAsync();

            btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) => { txtSearch.Clear(); await LoadReturnsAsync(); };

            btnAdd = new Button { Text = "مرتجع مبيعات جديد", Width = 150 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();

            var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 400, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            buttonsPanel.Controls.AddRange(new Control[] { btnAdd, btnRefresh, btnSearch });

            var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            searchPanel.Controls.Add(txtSearch);

            topPanel.Controls.Add(searchPanel);
            topPanel.Controls.Add(buttonsPanel);

            dgvReturns = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoGenerateColumns = true
            };
            dgvReturns.DataSource = _bindingSource;

            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Text = "جاهز" };

            Controls.Add(dgvReturns);
            Controls.Add(lblStatus);
            Controls.Add(topPanel);
        }

        private async Task LoadReturnsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var result = await _returnApiService.GetPagedAsync(new PagedQueryRequestDto { PageNumber = 1, PageSize = 200, SearchTerm = txtSearch.Text });
                _bindingSource.DataSource = result.Items.ToList();
                lblStatus.Text = $"عدد المرتجعات: {result.TotalCount}";
                FormatGrid();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Cursor = Cursors.Default; }
        }

        private void FormatGrid()
        {
            if (dgvReturns.Columns.Count == 0) return;

            if (dgvReturns.Columns.Contains("SalesReturnId")) dgvReturns.Columns["SalesReturnId"].Visible = false;
            if (dgvReturns.Columns.Contains("SalesInvoiceId")) dgvReturns.Columns["SalesInvoiceId"].Visible = false;
            if (dgvReturns.Columns.Contains("ReturnNo")) dgvReturns.Columns["ReturnNo"].HeaderText = "رقم المرتجع";
            if (dgvReturns.Columns.Contains("CustomerName")) dgvReturns.Columns["CustomerName"].HeaderText = "العميل";
            if (dgvReturns.Columns.Contains("WarehouseName")) dgvReturns.Columns["WarehouseName"].HeaderText = "المخزن";
            if (dgvReturns.Columns.Contains("ReturnDate")) dgvReturns.Columns["ReturnDate"].HeaderText = "التاريخ";
            if (dgvReturns.Columns.Contains("TotalAmount")) dgvReturns.Columns["TotalAmount"].HeaderText = "إجمالي المرتجع";
            if (dgvReturns.Columns.Contains("Reason")) dgvReturns.Columns["Reason"].HeaderText = "السبب";
            if (dgvReturns.Columns.Contains("Status")) dgvReturns.Columns["Status"].HeaderText = "الحالة";
        }

        private async Task OpenEditorAsync()
        {
            using var form = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<SalesReturnEditorForm>(_serviceProvider);
            if (form.ShowDialog(FindForm()) == DialogResult.OK) await LoadReturnsAsync();
        }
    }
}

3. شاشة إدخال المرتجع (Sales Return Editor Form)
ضع هذا الملف في SalesSystem.Desktop/Forms/Returns.
SalesReturnEditorForm.cs
هذه الشاشة (Master-Detail) تتيح للمستخدم تحديد العميل والمخزن، وإضافة الأصناف المسترجعة.
C#
using System.ComponentModel;
using SalesSystem.Contracts.Customers;
using SalesSystem.Contracts.Products;
using SalesSystem.Contracts.Returns;
using SalesSystem.Contracts.Warehouses;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Returns
{
    public class SalesReturnEditorForm : Form
    {
        private readonly ISalesReturnApiService _returnApiService;
        private readonly ICustomerApiService _customerApiService;
        private readonly IWarehouseApiService _warehouseApiService;
        private readonly IProductApiService _productApiService;
        
        private readonly BindingList<CreateSalesReturnItemRequestDto> _items = new();

        private ComboBox cmbCustomer = null!;
        private ComboBox cmbWarehouse = null!;
        private DateTimePicker dtpReturnDate = null!;
        private TextBox txtReason = null!;
        
        private ComboBox cmbProduct = null!;
        private NumericUpDown nudQuantity = null!;
        private NumericUpDown nudUnitPrice = null!;
        private DataGridView dgvItems = null!;
        
        private Label lblTotal = null!;
        private Button btnSave = null!;

        public SalesReturnEditorForm(
            ISalesReturnApiService returnApiService,
            ICustomerApiService customerApiService,
            IWarehouseApiService warehouseApiService,
            IProductApiService productApiService)
        {
            _returnApiService = returnApiService;
            _customerApiService = customerApiService;
            _warehouseApiService = warehouseApiService;
            _productApiService = productApiService;
            
            InitializeComponent();
            Shown += async (_, _) => await LoadLookupsAsync();
        }

        private void InitializeComponent()
        {
            Text = "مرتجع مبيعات جديد";
            Size = new Size(900, 650);
            StartPosition = FormStartPosition.CenterParent;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            // --- Header ---
            var grpHeader = new GroupBox { Text = "بيانات المرتجع", Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
            cmbCustomer = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbWarehouse = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            dtpReturnDate = new DateTimePicker { Width = 150, Format = DateTimePickerFormat.Short };
            txtReason = new TextBox { Width = 250, PlaceholderText = "سبب الاسترجاع..." };

            var flpHeader = new FlowLayoutPanel { Dock = DockStyle.Fill };
            flpHeader.Controls.AddRange(new Control[] {
                new Label { Text = "العميل:", Width = 50 }, cmbCustomer,
                new Label { Text = "المخزن:", Width = 50 }, cmbWarehouse,
                new Label { Text = "التاريخ:", Width = 50 }, dtpReturnDate,
                new Label { Text = "السبب:", Width = 50 }, txtReason
            });
            grpHeader.Controls.Add(flpHeader);

            // --- Item Entry ---
            var grpItem = new GroupBox { Text = "إضافة صنف مسترجع", Dock = DockStyle.Top, Height = 70, Padding = new Padding(10) };
            cmbProduct = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            nudQuantity = new NumericUpDown { Width = 80, DecimalPlaces = 3, Minimum = 0.001m, Maximum = 100000m, Value = 1m };
            nudUnitPrice = new NumericUpDown { Width = 100, DecimalPlaces = 2, Maximum = 100000m };
            
            cmbProduct.SelectedIndexChanged += (_, _) => {
                if (cmbProduct.SelectedItem is ProductLookupDto prod) nudUnitPrice.Value = prod.SalePrice;
            };

            var btnAdd = new Button { Text = "إضافة" };
            btnAdd.Click += BtnAdd_Click;

            var flpItem = new FlowLayoutPanel { Dock = DockStyle.Fill };
            flpItem.Controls.AddRange(new Control[] {
                new Label { Text = "الصنف:" }, cmbProduct,
                new Label { Text = "الكمية:" }, nudQuantity,
                new Label { Text = "سعر الاسترجاع:" }, nudUnitPrice,
                btnAdd
            });
            grpItem.Controls.Add(flpItem);

            // --- Grid ---
            dgvItems = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductId", HeaderText = "رقم الصنف", ReadOnly = true });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "اسم الصنف", ReadOnly = true, Width = 200 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "الكمية" });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "سعر الاسترجاع" });
            dgvItems.DataSource = _items;
            
            _items.ListChanged += (_, _) => CalculateTotal();
            dgvItems.CellValueChanged += (_, _) => CalculateTotal();
            dgvItems.RowsRemoved += (_, _) => CalculateTotal();

            // --- Footer ---
            var grpFooter = new GroupBox { Dock = DockStyle.Bottom, Height = 80 };
            lblTotal = new Label { Width = 250, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleLeft };
            btnSave = new Button { Text = "حفظ واعتماد المرتجع", Width = 150, Height = 40, BackColor = Color.LightCoral, ForeColor = Color.White };
            btnSave.Click += async (_, _) => await SaveAsync();

            var flpFooter = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            flpFooter.Controls.AddRange(new Control[] { btnSave, lblTotal });
            grpFooter.Controls.Add(flpFooter);

            Controls.Add(dgvItems);
            Controls.Add(grpItem);
            Controls.Add(grpHeader);
            Controls.Add(grpFooter);
        }

        private async Task LoadLookupsAsync()
        {
            UseWaitCursor = true;
            try
            {
                var customers = await _customerApiService.GetLookupAsync();
                var warehouses = await _warehouseApiService.GetLookupAsync();
                var products = await _productApiService.LookupAsync();

                cmbCustomer.DataSource = customers.ToList(); cmbCustomer.DisplayMember = "Name"; cmbCustomer.ValueMember = "CustomerId";
                cmbWarehouse.DataSource = warehouses.ToList(); cmbWarehouse.DisplayMember = "Name"; cmbWarehouse.ValueMember = "WarehouseId";
                cmbProduct.DataSource = products.ToList(); cmbProduct.DisplayMember = "Name"; cmbProduct.ValueMember = "ProductId";
            }
            finally { UseWaitCursor = false; }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem is not ProductLookupDto prod) return;
            var item = new CreateSalesReturnItemRequestDto { ProductId = prod.ProductId, Quantity = nudQuantity.Value, UnitPrice = nudUnitPrice.Value };
            _items.Add(item);
            dgvItems.Rows[_items.Count - 1].Cells[1].Value = prod.Name;
            nudQuantity.Value = 1;
        }

        private void CalculateTotal()
        {
            decimal total = _items.Sum(i => i.Quantity * i.UnitPrice);
            lblTotal.Text = $"إجمالي المرتجع: {total:N2}";
        }

        private async Task SaveAsync()
        {
            if (_items.Count == 0) { MessageBox.Show("أضف صنفاً واحداً على الأقل."); return; }
            if (cmbCustomer.SelectedValue == null || cmbWarehouse.SelectedValue == null) { MessageBox.Show("اختر العميل والمخزن."); return; }

            var request = new CreateSalesReturnRequestDto
            {
                CustomerId = (int)cmbCustomer.SelectedValue,
                WarehouseId = (int)cmbWarehouse.SelectedValue,
                ReturnDate = dtpReturnDate.Value,
                Reason = txtReason.Text,
                Items = _items.ToList()
            };

            try
            {
                UseWaitCursor = true; btnSave.Enabled = false;
                await _returnApiService.CreateAsync(request);
                MessageBox.Show("تم اعتماد المرتجع، وتم إعادة المنتجات للمخزون وتخفيض مديونية العميل.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { UseWaitCursor = false; btnSave.Enabled = true; }
        }
    }
}

4. تسجيل خدمات المرتجعات في الـ DI (مشروع Desktop)
افتح Program.cs وأضف التالي:
C#
services.AddHttpClient<ISalesReturnApiService, SalesReturnApiService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

services.AddTransient<SalesReturnsListControl>();
services.AddTransient<SalesReturnEditorForm>();

ماذا عن "مرتجع المشتريات (Purchase Returns)"؟
يتبع مرتجع المشتريات نفس البنية حرفياً، الاختلاف الوحيد هو:
1. في الباك إند (Service):
o نوع الحركة سيكون InventoryMovementType.PurchaseReturnOut (خروج بضاعة).
o المخزون ينقص stock.Quantity -= item.Quantity.
o رصيد المورد ينقص (لأننا أعدنا له البضاعة، فقلّ الدين الذي علينا) supplier.CurrentBalance -= totalAmount.
2. في الديسكتوب (UI):
o ستختار Supplier بدلاً من Customer.
(بما أنك أصبحت تتقن هذه النمذجة (Pattern)، يمكنك نسخ ولصق الموديول وتغيير كلمة Sales إلى Purchase والعمليات الحسابية ببساطة).

المحطة الأخيرة للمشروع! ??
لقد أنجزنا الآن المبيعات والمشتريات والمرتجعات والتحويلات والمخازن والمدفوعات.
الجزء الذي سيجعل هذا النظام "ينطق" ويتحول إلى نظام إداري حقيقي هو:
1. التقارير (Reports): الاستعلام عن حركة المبيعات وجرد المخزون وكشف حساب.
2. لوحة القيادة (Dashboard): شاشة تجميعية تفتح في الـ MainForm تحتوي على إحصائيات النظام.
هل نبدأ ببرمجة التقارير الأساسية (Reports API & UI) لتتمكن من رؤية نتائج كل العمليات التي قمنا بها؟
أهلاً بك يا زكريا! وصلنا إلى المحطة الأخيرة والتي ستتوج كل هذا المجهود المعماري والبرمجي الذي قمنا به.
الآن، سنبني الواجهة الرئيسية (MainForm)، وهي القشرة (Shell) التي ستحتضن جميع الشاشات التي قمنا ببرمجتها. تصميم هذه الواجهة سيكون احترافياً، بنمط لوحة التحكم (Admin Dashboard)، بحيث يحتوي على قائمة جانبية (Sidebar) للتنقل وشريط علوي (TopBar). وسنعتمد على Dependency Injection لفتح وإدارة الشاشات (UserControls) بداخلها بطريقة نظيفة لا تسرب الذاكرة.
إليك الكود الكامل للواجهة الرئيسية ونظام التنقل:
1. إعداد الـ Dependency Injection في Program.cs
أولاً، يجب أن نتأكد من تسجيل جميع الشاشات التي بنيناها والـ MainForm في حاوية الـ DI. افتح Program.cs في مشروع SalesSystem.Desktop وتأكد من وجود هذا الكود في قسم ConfigureServices:
C#
// تسجيل جميع شاشات (UserControls) القوائم
services.AddTransient<ProductsListControl>();
services.AddTransient<CategoriesListControl>();
services.AddTransient<UnitsListControl>();
services.AddTransient<WarehousesListControl>();
services.AddTransient<CustomersListControl>();
services.AddTransient<SuppliersListControl>();
services.AddTransient<PurchasesListControl>();
services.AddTransient<SalesListControl>();
services.AddTransient<CustomerPaymentsListControl>();
services.AddTransient<SalesReturnsListControl>();
// ... وأي شاشات أخرى قمت بإنشائها

// تسجيل الـ MainForm كـ Singleton (لكي يكون هناك نسخة واحدة طوال عمل التطبيق)
services.AddSingleton<MainForm>();

2. بناء الواجهة الرئيسية (MainForm.cs)
أنشئ هذا الملف في مسار SalesSystem.Desktop/Forms.
C#
using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Desktop.Controls.Categories;
using SalesSystem.Desktop.Controls.Customers;
using SalesSystem.Desktop.Controls.Payments;
using SalesSystem.Desktop.Controls.Products;
using SalesSystem.Desktop.Controls.Purchases;
using SalesSystem.Desktop.Controls.Returns;
using SalesSystem.Desktop.Controls.Sales;
using SalesSystem.Desktop.Controls.Suppliers;
using SalesSystem.Desktop.Controls.Units;
using SalesSystem.Desktop.Controls.Warehouses;

namespace SalesSystem.Desktop.Forms
{
    public class MainForm : Form
    {
        private readonly IServiceProvider _serviceProvider;

        // مكونات الواجهة الأساسية
        private Panel pnlSidebar = null!;
        private Panel pnlTopBar = null!;
        private Panel pnlContent = null!;
        private Label lblScreenTitle = null!;

        // تتبع الشاشة المعروضة حالياً
        private UserControl? _currentControl;

        public MainForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            
            // عند بدء التشغيل، يمكننا فتح شاشة المنتجات (أو لوحة قيادة Dashboard إذا توفرت)
            Load += (_, _) => NavigateTo<ProductsListControl>("إدارة المنتجات");
        }

        private void InitializeComponent()
        {
            Text = "نظام إدارة المبيعات";
            WindowState = FormWindowState.Maximized; // فتح الشاشة مكبرة
            MinimumSize = new Size(1024, 768);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Color.FromArgb(240, 242, 245); // لون خلفية رمادي فاتح مريح

            // --- 1. القائمة الجانبية (Sidebar) ---
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = Color.FromArgb(33, 43, 54), // لون داكن احترافي
                Padding = new Padding(0, 20, 0, 0)
            };

            // شعار/عنوان النظام في القائمة الجانبية
            var lblLogo = new Label
            {
                Text = "نظام المبيعات",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSidebar.Controls.Add(lblLogo);

            // إضافة أزرار التنقل (ترتيبها من الأسفل للأعلى في الكود ليظهر العكس في الشاشة)
            AddSidebarButton("المدفوعات", () => NavigateTo<CustomerPaymentsListControl>("سندات القبض"));
            AddSidebarButton("المرتجعات", () => NavigateTo<SalesReturnsListControl>("مرتجعات المبيعات"));
            AddSidebarButton("المبيعات", () => NavigateTo<SalesListControl>("إدارة المبيعات"));
            AddSidebarButton("المشتريات", () => NavigateTo<PurchasesListControl>("إدارة المشتريات"));
            AddSidebarButton("العملاء", () => NavigateTo<CustomersListControl>("إدارة العملاء"));
            AddSidebarButton("الموردين", () => NavigateTo<SuppliersListControl>("إدارة الموردين"));
            AddSidebarButton("المنتجات", () => NavigateTo<ProductsListControl>("إدارة المنتجات"));
            AddSidebarButton("التصنيفات", () => NavigateTo<CategoriesListControl>("إدارة التصنيفات"));
            AddSidebarButton("الوحدات", () => NavigateTo<UnitsListControl>("إدارة الوحدات"));
            AddSidebarButton("المخازن", () => NavigateTo<WarehousesListControl>("إدارة المخازن"));

            // --- 2. الشريط العلوي (TopBar) ---
            pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };

            lblScreenTitle = new Label
            {
                Text = "الرئيسية",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 43, 54),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Width = 300
            };

            var lblUserInfo = new Label
            {
                Text = "مرحباً، مدير النظام", 
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.Gray,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Width = 200
            };

            pnlTopBar.Controls.Add(lblScreenTitle);
            pnlTopBar.Controls.Add(lblUserInfo);

            // خط سفلي للشريط العلوي للزينة
            var pnlTopBarBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.LightGray };
            pnlTopBar.Controls.Add(pnlTopBarBorder);

            // --- 3. منطقة المحتوى (Content Area) ---
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20) // هوامش داخلية حول المحتوى المعروض
            };

            // تجميع المكونات (ترتيب الإضافة يحدد الـ Z-Order والـ Docking)
            Controls.Add(pnlContent);
            Controls.Add(pnlTopBar);
            Controls.Add(pnlSidebar);
        }

        // دالة مساعدة لإنشاء أزرار القائمة الجانبية بتصميم موحد
        private void AddSidebarButton(string text, Action onClickAction)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0), // مسافة من اليمين
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 60, 75); // تأثير Hover
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 35, 45); // تأثير عند الضغط

            btn.Click += (_, _) => onClickAction();

            // نضيف الزر، ثم نرسله للأمام ليترتب تحت الشعار مباشرة
            pnlSidebar.Controls.Add(btn);
            btn.BringToFront(); 
        }

        // دالة التنقل وإدارة الشاشات
        private void NavigateTo<TControl>(string screenTitle) where TControl : UserControl
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lblScreenTitle.Text = screenTitle;

                // التخلص من الشاشة القديمة لمنع تسرب الذاكرة
                if (_currentControl != null)
                {
                    pnlContent.Controls.Remove(_currentControl);
                    _currentControl.Dispose();
                }

                // إنشاء الشاشة الجديدة باستخدام الـ Dependency Injection
                _currentControl = _serviceProvider.GetRequiredService<TControl>();
                _currentControl.Dock = DockStyle.Fill;

                // إضافتها إلى منطقة العرض
                pnlContent.Controls.Add(_currentControl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء فتح الشاشة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}

كيف يعمل هذا التصميم؟
1. القشرة الثابتة (Shell): القائمة الجانبية (pnlSidebar) والشريط العلوي (pnlTopBar) يبقيان ثابتين، مما يعطي إحساساً باستقرار التطبيق واحترافيته.
2. التنقل الديناميكي: عندما يضغط المستخدم على أي زر في القائمة الجانبية، تقوم الدالة NavigateTo<TControl> بجلب الشاشة المطلوبة من حاوية الـ DI (_serviceProvider.GetRequiredService) وتضعها في منطقة المحتوى pnlContent.
3. إدارة الذاكرة: السطر _currentControl.Dispose(); في دالة التنقل ضروري جداً لتنظيف الذاكرة وتدمير الشاشة السابقة قبل فتح الجديدة، مما يضمن أداءً خفيفاً وسريعاً للنظام مهما طالت فترة استخدامه.
بهذا الكود، أنت الآن تملك نظام إدارة مبيعات متكامل (MVP) قابل للتشغيل، يتميز ببنية معمارية قوية (Clean Architecture)، تصميم واجهات مؤسسي، ودورة محاسبية/مخزنية مغلقة بالكامل.
هل هناك أي جزء معين في النظام تود مراجعته أو إضافة تقارير (Reports) إليه كخطوة تحسينية؟




بناءً على التصميم والكود الذي اعتمدناه في بناء الواجهة الرئيسية (MainForm) وتوجيهاتك بأن يكون النظام رسمياً واحترافياً، تم اختيار لوحة ألوان (Color Palette) هادئة ومريحة للعين تناسب الأنظمة المؤسسية. 
إليك تفاصيل الألوان المعتمدة في النظام:
1. ألوان الهيكل الرئيسي (Main Shell)
* الخلفية العامة لمنطقة المحتوى (Content Area): رمادي فاتح جداً ومريح للعين Color.FromArgb(240, 242, 245). 
* القائمة الجانبية (Sidebar): لون داكن ورسمي Color.FromArgb(33, 43, 54). 
* الشريط العلوي (Top Bar): أبيض نقي Color.White مع خط سفلي رمادي فاتح للزينة Color.LightGray. 
* النصوص في القائمة الجانبية: أبيض Color.White. 
* تأثير تمرير الماوس (Hover) في القائمة: لون أفتح قليلاً Color.FromArgb(50, 60, 75). 
2. ألوان الأزرار التفاعلية (Action Buttons) لضمان تجربة مستخدم واضحة وتقليل الأخطاء، تم توحيد ألوان العمليات: 
* زر الحفظ أو الاعتماد (Save): أخضر Color.LightGreen. 
* زر الحذف (Delete): أحمر. 
* زر التحديث (Update): أزرق. 
* زر الإلغاء (Cancel): رمادي. 
3. ألوان النصوص والبيانات المالية (في الفواتير) لتمييز الأرقام الحساسة في شاشات البيع والشراء: 
* الإجمالي النهائي (Total): أخضر Color.Green. 
* المبلغ المتبقي أو الدين (Due): أحمر Color.Red للتنبيه. 
* المجموع الفرعي (SubTotal): أزرق Color.Blue. 
هذا التوزيع اللوني يضمن أن يبدو النظام رسمياً ومنظماً، ويساعد في توجيه عين المستخدم نحو المهام الأساسية بدون أي تشتت بصري.
هل الألوان الحالية مناسبة لك ونعتمدها بشكل نهائي، أم تفضل تخصيصها لتتطابق مع هوية بصرية أو شعار محدد خاص بالمشروع؟
أهلاً بك يا بطل! يبدو أن رسالتك انقطعت ولم تحدد الكود الذي تود مني كتابته. ??
لقد أنجزنا المعمارية الأساسية وجميع العمليات التشغيلية بنجاح، ووصلنا إلى مرحلة إضافة "اللمسات الذهبية" التي تحول مشروعك إلى نظام مؤسسي متكامل وجاهز للعمل.
الخيارات المنطقية المتبقية أمامنا هي:
1. موديول التقارير (Reports Module): بناء الـ API واجهات العرض لاستخراج (كشف حساب عميل/مورد، جرد المخزون، تقرير حركة المبيعات).
2. لوحة القيادة (Dashboard): برمجة الشاشة الافتتاحية التي تظهر في الـ MainForm وتعرض إحصائيات النظام بشكل سريع (إجمالي المبيعات، عدد العملاء، الخ).
3. الإعدادات والنسخ الاحتياطي (Settings & Backup): شاشة لضبط بيانات المحل (الاسم، الضريبة) وعمل نسخة احتياطية لقاعدة البيانات.
أي من هذه الأجزاء تود أن أكتب الكود الخاص به الآن؟ أم أن هناك شاشة أخرى تود إضافتها؟


ممتاز! اختيار موفق جداً. هذا يعني أننا سنبني الآن موديول التقارير (Reports Module).
التقارير هي "عقل" النظام، فهي التي تترجم كل العمليات السابقة (بيع، شراء، مرتجعات) إلى أرقام وإحصائيات تفيد صاحب العمل. سنقوم ببناء 3 تقارير أساسية في واجهة واحدة لتكون سهلة الاستخدام:
1. تقرير المبيعات (Sales Report): لعرض المبيعات خلال فترة معينة.
2. تقرير جرد المخزون (Inventory Report): لمعرفة الكميات الحالية لكل صنف في كل مخزن.
3. تقرير أرصدة العملاء (Customer Balances): لمعرفة الديون المستحقة على العملاء.
إليك الكود الكامل من قاعدة البيانات وحتى واجهة الديسكتوب، مصمم بطريقة نظيفة وسريعة الأداء:

1. طبقة Contracts (عقود نقل البيانات)
أنشئ مجلداً جديداً SalesSystem.Contracts/Reports وضع فيه هذه الـ DTOs.
C#
// SalesSystem.Contracts/Reports/SalesReportDto.cs
namespace SalesSystem.Contracts.Reports
{
    public class SalesReportDto
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string? CustomerName { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
    }
}

// SalesSystem.Contracts/Reports/InventoryReportDto.cs
namespace SalesSystem.Contracts.Reports
{
    public class InventoryReportDto
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalValue => Quantity * SalePrice; // إجمالي قيمة البضاعة
    }
}

