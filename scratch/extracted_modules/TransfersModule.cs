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
// تحديث IAuthService لتشمل الدالة الجديدة
public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
}

// داخل كلاس AuthService
public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
    
    if (user == null || !user.IsActive)
        return Result<AuthResponseDto>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة.");

    // استخدام BCrypt للتحقق من تطابق كلمة المرور المدخلة مع الـ Hash
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) 
        return Result<AuthResponseDto>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة.");

    var token = GenerateJwtToken(user);
    return Result<AuthResponseDto>.Success(new AuthResponseDto { Token = token, FullName = user.FullName, Role = (int)user.Role });
}

public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
{
    // نفترض إضافة دالة GetByIdAsync لـ IUserRepository
    var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
    if (user == null) return Result.Failure("المستخدم غير موجود.");

    if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        return Result.Failure("كلمة المرور القديمة غير صحيحة.");

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    
    // حفظ التغييرات عبر IUnitOfWork (تأكد من حقنها في الـ Constructor)
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Result.Success();
}
د) تحديث الـ API (AuthController.cs)
أضف الـ Endpoint الخاص بتغيير كلمة المرور. لاحظ استخدام [Authorize] للحماية:
C#
[Authorize] // يتطلب تسجيل الدخول
[HttpPost("change-password")]
public async Task<ActionResult<ApiResponseDto<object>>> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
{
    // استخراج معرّف المستخدم (ID) من التوكن
    var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(userIdClaim, out int userId))
        return Unauthorized(ApiResponseDto<object>.Fail("جلسة غير صالحة."));

    var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
    
    if (result.IsFailure) return BadRequest(ApiResponseDto<object>.Fail(result.Error ?? "فشل تغيير كلمة المرور"));
    return Ok(ApiResponseDto<object>.Success(null, "تم تغيير كلمة المرور بنجاح."));
}

2. إدارة الحساب في الديسكتوب (Change Password UI)
سنقوم ببناء شاشة رسمية وبسيطة تتماشى مع هوية النظام لتغيير كلمة المرور.
أ) تحديث AuthApiService.cs
أضف دالة استدعاء الـ API:
C#
public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request)
{
    var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiResponseDto<object>>();
        throw new InvalidOperationException(error?.Message ?? "فشل تغيير كلمة المرور.");
    }
    return true;
}
ب) برمجة شاشة ChangePasswordForm.cs
ضعها في SalesSystem.Desktop/Forms:
C#
using SalesSystem.Contracts.Auth;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms
{
    public class ChangePasswordForm : Form
    {
        private readonly AuthApiService _authApi;
        private TextBox txtOldPassword = null!;
        private TextBox txtNewPassword = null!;
        private TextBox txtConfirmPassword = null!;
        private Button btnSave = null!;
