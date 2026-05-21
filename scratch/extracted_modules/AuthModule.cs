    public class LoginForm : Form
    {
        private readonly AuthApiService _authApiService;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;

        public LoginForm(AuthApiService authApiService)
        {
            _authApiService = authApiService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "تسجيل الدخول";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
            BackColor = Color.White;

            var lblTitle = new Label { Text = "نظام إدارة المبيعات", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 60 };
            
            txtUsername = new TextBox { Width = 250, PlaceholderText = "اسم المستخدم" };
            txtPassword = new TextBox { Width = 250, PlaceholderText = "كلمة المرور", UseSystemPasswordChar = true };
            
            btnLogin = new Button { Text = "دخول", Width = 250, Height = 40, BackColor = Color.FromArgb(33, 43, 54), ForeColor = Color.White };
            btnLogin.Click += async (_, _) => await PerformLoginAsync();
            AcceptButton = btnLogin;

            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(60, 20, 20, 20) };
            flp.Controls.AddRange(new Control[] { new Label { Text = "اسم المستخدم:" }, txtUsername, new Label { Text = "كلمة المرور:" }, txtPassword, new Label { Height = 10 }, btnLogin });

            Controls.Add(flp);
            Controls.Add(lblTitle);
        }

        private async Task PerformLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم المستخدم وكلمة المرور."); return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                btnLogin.Enabled = false;

                var result = await _authApiService.LoginAsync(new LoginRequestDto
                {
                    UserName = txtUsername.Text,
                    Password = txtPassword.Text
                });

                if (result != null)
                {
                    TokenStore.Token = result.Token;
                    TokenStore.CurrentUserName = result.FullName;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Cursor = Cursors.Default; btnLogin.Enabled = true; }
        }
    }
}
تعديل Program.cs في الـ Desktop لتشغيل الـ Login أولاً وإرفاق الـ Token
في SalesSystem.Desktop/Program.cs:
C#
// 1. إنشاء Handler لإرفاق التوكن مع كل طلب
public class JwtAuthorizationHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(TokenStore.Token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenStore.Token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

// 2. داخل دالة Main أو ConfigureServices:
services.AddTransient<JwtAuthorizationHandler>();

// 3. تعديل تسجيل الـ HttpClients لكي تستخدم الـ Handler
services.AddHttpClient<AuthApiService>(c => c.BaseAddress = new Uri(baseUrl)); // Auth لا يحتاج توكن
services.AddHttpClient<IProductApiService, ProductApiService>(c => c.BaseAddress = new Uri(baseUrl))
        .AddHttpMessageHandler<JwtAuthorizationHandler>();
// (قم بتطبيق AddHttpMessageHandler على بقية الـ Services)

services.AddTransient<LoginForm>();

// 4. في دالة Main، شغل الـ LoginForm أولاً:
var loginForm = host.Services.GetRequiredService<LoginForm>();
if (loginForm.ShowDialog() == DialogResult.OK)
{
    var mainForm = host.Services.GetRequiredService<MainForm>();
    Application.Run(mainForm);
}
else
{
    Application.Exit();
}
ماذا أنجزنا الآن؟
لقد قمنا بسد أكبر فجوة أمنية ومعمارية في النظام. الآن لا يمكن لأي شخص الدخول للبرنامج أو التلاعب بالـ API دون تسجيل دخول وتوكن صالح.
(تذكر: اسم المستخدم الافتراضي الذي زرعناه في قاعدة البيانات هو admin وكلمة المرور CHANGE_ME_HASH)
بهذا نكون أغلقنا النقطة الثالثة من نواقص الـ Code Review. هل ننتقل إلى النقطة الرابعة والأخيرة وهي (Desktop UI Gaps: كشف الحساب التفصيلي للعملاء وشاشة عرض الفاتورة)؟

ممتاز جداً! وصلنا إلى مسك الختام. إضافة كشف الحساب التفصيلي وشاشة عرض الفاتورة هي ما يعطي النظام طابعه المحاسبي والتدقيقي الاحترافي.
سنقوم ببناء هذين المكونين خطوة بخطوة:
أولاً: كشف الحساب التفصيلي للعميل (Customer Ledger)
كشف الحساب يجمع كل حركات العميل (فواتير المبيعات، سندات القبض، مرتجعات المبيعات) ويرتبها زمنياً مع حساب الرصيد التراكمي.
1. طبقة Contracts (عقود البيانات)
أضف هذا الكلاس في SalesSystem.Contracts/Reports/CustomerLedgerEntryDto.cs:
C#
namespace SalesSystem.Contracts.Reports
{
    public class CustomerLedgerEntryDto
    {
        public DateTime Date { get; set; }
        public string TransactionType { get; set; } = string.Empty; // نوع الحركة: فاتورة، سند، مرتجع
        public string ReferenceNo { get; set; } = string.Empty; // رقم المستند
        public decimal Debit { get; set; } // مدين (عليه)
        public decimal Credit { get; set; } // دائن (له)
        public decimal Balance { get; set; } // الرصيد المتراكم
    }
}
2. طبقة Infrastructure (استخراج البيانات)
افتح ReportRepository.cs وأضف هذه الدالة (مع إضافتها لـ IReportRepository):
C#
public async Task<IReadOnlyList<CustomerLedgerEntryDto>> GetCustomerLedgerAsync(int customerId, CancellationToken cancellationToken = default)
{
    // 1. جلب فواتير المبيعات (مدين - تزيد الرصيد)
    var invoices = await _context.SalesInvoices
        .Where(x => x.CustomerId == customerId && x.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled)
        .Select(x => new { Date = x.InvoiceDate, No = x.InvoiceNo, Type = "فاتورة مبيعات", Debit = x.TotalAmount, Credit = 0m })
        .ToListAsync(cancellationToken);

    // 2. جلب سندات القبض (دائن - تنقص الرصيد)
    var payments = await _context.CustomerPayments
        .Where(x => x.CustomerId == customerId)
        .Select(x => new { Date = x.PaymentDate, No = x.PaymentNo, Type = "سند قبض", Debit = 0m, Credit = x.Amount })
        .ToListAsync(cancellationToken);

    // 3. جلب المرتجعات (دائن - تنقص الرصيد)
    var returns = await _context.SalesReturns
        .Where(x => x.CustomerId == customerId && x.Status != SalesSystem.Domain.Enums.InvoiceStatus.Cancelled)
        .Select(x => new { Date = x.ReturnDate, No = x.ReturnNo, Type = "مرتجع مبيعات", Debit = 0m, Credit = x.TotalAmount })
        .ToListAsync(cancellationToken);

    // دمج الحركات وترتيبها زمنياً
    var combined = invoices
        .Concat(payments)
        .Concat(returns)
        .OrderBy(x => x.Date)
        .Select(x => new CustomerLedgerEntryDto
        {
            Date = x.Date,
            ReferenceNo = x.No,
            TransactionType = x.Type,
            Debit = x.Debit,
            Credit = x.Credit
        }).ToList();

    // حساب الرصيد التراكمي
    decimal runningBalance = 0;
    foreach (var entry in combined)
    {
        runningBalance += (entry.Debit - entry.Credit);
        entry.Balance = runningBalance;
    }

    return combined;
}
3. طبقة Application و API
* في IReportService و ReportService، أضف دالة GetCustomerLedgerAsync التي تستدعي ?ـ Repository.
* في ReportsController، أضف Endpoint جديد:
C#
[HttpGet("customer-ledger/{customerId:int}")]
public async Task<ActionResult<ApiResponseDto<IReadOnlyList<CustomerLedgerEntryDto>>>> GetCustomerLedger(int customerId, CancellationToken cancellationToken)
{
    var result = await _reportService.GetCustomerLedgerAsync(customerId, cancellationToken);
    return Ok(ApiResponseDto<IReadOnlyList<CustomerLedgerEntryDto>>.Success(result.Data!));
}
4. طبقة Desktop (واجهة كشف الحساب)
* أضف الدالة للـ IReportApiService و ReportApiService.
* أنشئ فورم جديد CustomerLedgerForm.cs في SalesSystem.Desktop/Forms/Reports:
C#
using SalesSystem.Contracts.Reports;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms.Reports
{
    public class CustomerLedgerForm : Form
    {
