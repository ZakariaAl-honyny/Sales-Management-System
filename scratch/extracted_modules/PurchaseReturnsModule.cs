    public class PurchaseReturnsListControl : UserControl
    {
        private readonly IPurchaseReturnApiService _returnApiService; // يجب إنشاؤها بنفس نمط Sales
        private readonly IServiceProvider _serviceProvider;
        private readonly BindingSource _bindingSource = new();

        private TextBox txtSearch = null!;
        private DataGridView dgvReturns = null!;

        public PurchaseReturnsListControl(IPurchaseReturnApiService returnApiService, IServiceProvider serviceProvider)
        {
            _returnApiService = returnApiService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            Load += async (_, _) => await LoadReturnsAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes;
            
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };
            txtSearch = new TextBox { Width = 260, PlaceholderText = "ابحث برقم المرتجع..." };
            
            var btnSearch = new Button { Text = "بحث", Width = 80 };
            btnSearch.Click += async (_, _) => await LoadReturnsAsync();

            var btnAdd = new Button { Text = "مرتجع مشتريات جديد", Width = 150 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();

            var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            searchPanel.Controls.AddRange(new Control[] { btnAdd, btnSearch, txtSearch });
            topPanel.Controls.Add(searchPanel);

            dgvReturns = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoGenerateColumns = true
            };
            dgvReturns.DataSource = _bindingSource;

            Controls.Add(dgvReturns);
            Controls.Add(topPanel);
        }

        private async Task LoadReturnsAsync()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var result = await _returnApiService.GetPagedAsync(new PagedQueryRequestDto { PageNumber = 1, PageSize = 100, SearchTerm = txtSearch.Text });
                _bindingSource.DataSource = result.Items.ToList();
                FormatGrid();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ"); }
            finally { Cursor = Cursors.Default; }
        }

        private void FormatGrid()
        {
            if (dgvReturns.Columns.Count == 0) return;
            if (dgvReturns.Columns.Contains("ReturnNo")) dgvReturns.Columns["ReturnNo"].HeaderText = "رقم المرتجع";
            if (dgvReturns.Columns.Contains("SupplierName")) dgvReturns.Columns["SupplierName"].HeaderText = "المورد";
            if (dgvReturns.Columns.Contains("TotalAmount")) dgvReturns.Columns["TotalAmount"].HeaderText = "الإجمالي";
            if (dgvReturns.Columns.Contains("ReturnDate")) dgvReturns.Columns["ReturnDate"].HeaderText = "التاريخ";
            // إخفاء الأعمدة غير الضرورية كالـ IDs
        }

        private async Task OpenEditorAsync()
        {
            using var form = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PurchaseReturnEditorForm>(_serviceProvider);
            if (form.ShowDialog(FindForm()) == DialogResult.OK) await LoadReturnsAsync();
        }
    }
}

3. شاشة قائمة التحويلات المخزنية (Stock Transfers List)
هذه الشاشة تعرض سجل البضاعة التي نُقلت من مخزن إلى آخر.
TransfersListControl.cs
ضعها في SalesSystem.Desktop/Controls/Warehouses:
C#
using SalesSystem.Contracts.Common;
using SalesSystem.Desktop.Forms.Warehouses; // نفترض وجود TransferEditorForm هنا
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Controls.Warehouses
{
    public class TransfersListControl : UserControl
    {
        private readonly IStockTransferApiService _transferApiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BindingSource _bindingSource = new();

        private DataGridView dgvTransfers = null!;

        public TransfersListControl(IStockTransferApiService transferApiService, IServiceProvider serviceProvider)
        {
            _transferApiService = transferApiService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            Load += async (_, _) => await LoadTransfersAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };
            
            var btnAdd = new Button { Text = "عملية تحويل جديدة", Width = 150 };
            btnAdd.Click += async (_, _) => await OpenEditorAsync();
            
            var btnRefresh = new Button { Text = "تحديث", Width = 80 };
            btnRefresh.Click += async (_, _) => await LoadTransfersAsync();

            var flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            flowPanel.Controls.AddRange(new Control[] { btnAdd, btnRefresh });
            topPanel.Controls.Add(flowPanel);

            dgvTransfers = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoGenerateColumns = true
            };
            dgvTransfers.DataSource = _bindingSource;

            Controls.Add(dgvTransfers);
            Controls.Add(topPanel);
        }

        private async Task LoadTransfersAsync()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var result = await _transferApiService.GetPagedAsync(new PagedQueryRequestDto { PageNumber = 1, PageSize = 100 });
                _bindingSource.DataSource = result.Items.ToList();
                FormatGrid();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ"); }
            finally { Cursor = Cursors.Default; }
        }

        private void FormatGrid()
        {
            if (dgvTransfers.Columns.Count == 0) return;
            if (dgvTransfers.Columns.Contains("TransferNo")) dgvTransfers.Columns["TransferNo"].HeaderText = "رقم التحويل";
            if (dgvTransfers.Columns.Contains("FromWarehouseName")) dgvTransfers.Columns["FromWarehouseName"].HeaderText = "من مخزن";
            if (dgvTransfers.Columns.Contains("ToWarehouseName")) dgvTransfers.Columns["ToWarehouseName"].HeaderText = "إلى مخزن";
            if (dgvTransfers.Columns.Contains("TransferDate")) dgvTransfers.Columns["TransferDate"].HeaderText = "التاريخ";
            if (dgvTransfers.Columns.Contains("Status")) dgvTransfers.Columns["Status"].HeaderText = "الحالة";
        }

        private async Task OpenEditorAsync()
        {
            // TransferEditorForm هي الشاشة التي برمجناها مسبقاً لإجراء التحويل
            using var form = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<TransferEditorForm>(_serviceProvider);
            if (form.ShowDialog(FindForm()) == DialogResult.OK) await LoadTransfersAsync();
        }
    }
}

تحديث الـ MainForm
لتكتمل المرحلة الأولى، قم بإضافة زري (مرتجع المشتريات) و(التحويلات المخزنية) إلى القائمة الجانبية (Sidebar) في ملف MainForm.cs الذي كتبناه سابقاً:
C#
AddSidebarButton("مرتجعات المشتريات", () => NavigateTo<PurchaseReturnsListControl>("مرتجعات المشتريات"));
AddSidebarButton("تحويلات المخازن", () => NavigateTo<TransfersListControl>("إدارة التحويلات بين المخازن"));
(تذكر: تأكد من تسجيل هذه الـ Controls كـ Transient في ملف Program.cs الخاص بالديسكتوب).
بهذا نكون قد أتممنا المرحلة الأولى 100%، وأصبحت جميع شاشات الدورة المحاسبية والمخزنية موجودة ومرتبطة ببعضها! ??
هل ننتقل فوراً إلى المرحلة الثانية (تشفير كلمات المرور، إدارة الحساب، وتسجيل الخروج) لضمان أمان النظام قبل الختام؟








أهلاً بك في المرحلة الثانية، وهي الجدار الواقي للنظام!
في أي نظام مؤسسي ورسمي، يجب ألا تُخزن كلمات المرور كنصوص مكشوفة أبداً، ويجب أن يمتلك المستخدم القدرة على تغيير كلمة المرور الخاصة به، بالإضافة إلى إمكانية تسجيل الخروج بشكل آمن لإنهاء الجلسة.
لنبدأ بتنفيذ هذه المرحلة بخطوات عملية ومباشرة:
1. تشفير كلمات المرور (BCrypt)
أولاً، ستحتاج إلى تثبيت حزمة التشفير القياسية. في مشروع SalesSystem.Application ومشروع SalesSystem.Infrastructure (إذا كان يحتوي على كود زرع البيانات Seed)، قم بتثبيت حزمة NuGet التالية:
BCrypt.Net-Next
أ) تعديل زرع البيانات (Database Initializer)
عند إنشاء أول مستخدم (Admin) في قاعدة البيانات، يجب تشفير كلمة المرور الافتراضية هكذا:
C#
// بدلاً من إعطائه نصاً عادياً، نستخدم دالة التشفير:
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"); 
ب) إضافة عقد تغيير كلمة المرور (Contracts)
في مجلد SalesSystem.Contracts/Auth، أضف:
C#
public class ChangePasswordRequestDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
ج) تحديث خدمة المصادقة (AuthService.cs)
افتح AuthService.cs وقم بتحديث دالة LoginAsync لفك التشفير، وأضف دالة ChangePasswordAsync:
C#
