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

// SalesSystem.Contracts/Reports/CustomerBalanceDto.cs
namespace SalesSystem.Contracts.Reports
{
    public class CustomerBalanceDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal CurrentBalance { get; set; } // الدين المستحق
    }
}

2. طبقة Application & Infrastructure (جلب البيانات)
بما أن التقارير هي عمليات "قراءة فقط" (Read-Only)، سنقوم بجلبها مباشرة عبر الـ Repository بأداء عالٍ باستخدام AsNoTracking و Select لتجنب تحميل كائنات كاملة في الذاكرة.
IReportRepository.cs (في Abstractions/Persistence)
C#
using SalesSystem.Contracts.Reports;

namespace SalesSystem.Application.Abstractions.Persistence
{
    public interface IReportRepository
    {
        Task<IReadOnlyList<SalesReportDto>> GetSalesReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InventoryReportDto>> GetInventoryReportAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerBalanceDto>> GetCustomerBalancesAsync(CancellationToken cancellationToken = default);
    }
}
ReportRepository.cs (في Infrastructure/Persistence/Repositories)
C#
using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Abstractions.Persistence;
using SalesSystem.Contracts.Reports;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly SalesSystemDbContext _context;

        public ReportRepository(SalesSystemDbContext context)
        {
            _context = context;
        }

