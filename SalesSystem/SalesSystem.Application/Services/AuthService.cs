using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Implementation of the authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork uow,
        IJwtTokenGenerator jwtTokenGenerator,
        JwtSettings jwtSettings,
        IAuditLogService auditLogService,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Login attempt for user: {UserName}", request.UserName);

            // 1. Find user
            var users = await _uow.Users.GetAllAsync(ct);
            var user = users.FirstOrDefault(u => u.UserName.Equals(request.UserName, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {UserName} not found", request.UserName);
                return Result<LoginResponse>.Failure("بيانات الاعتماد غير صالحة", ErrorCodes.Unauthorized);
            }

            // 2. Check if account is locked
            if (user.Status == UserStatus.Locked)
            {
                _logger.LogWarning("Login failed: User {UserName} account is locked", request.UserName);
                await _auditLogService.LogAsync(user.Id, "LoginFailed_Locked", "User", user.Id,
                    "محاولة دخول لحساب مقفول", null, ct);
                return Result<LoginResponse>.Failure("الحساب مقفول — يرجى الاتصال بالمسؤول", ErrorCodes.AccountLocked);
            }

            // 3. Check if account is active (not inactive/soft-deleted)
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Login failed: User {UserName} is inactive", request.UserName);
                return Result<LoginResponse>.Failure("الحساب معطل", ErrorCodes.Forbidden);
            }

            // 4. Check if password hash is null (passwordless setup required)
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _logger.LogWarning("Login failed: User {UserName} has no password set", request.UserName);
                return Result<LoginResponse>.Failure("يجب تعيين كلمة المرور أولاً", ErrorCodes.RequiresPasswordSetup);
            }

            // 5. Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            // 6. Record login attempt (always, regardless of success/failure)
            user.RecordLoginAttempt(isPasswordValid);
            await _uow.SaveChangesAsync(ct);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Incorrect password for user {UserName} (attempt {LoginAttempts})",
                    request.UserName, user.LoginAttempts);
                await _auditLogService.LogAsync(user.Id, "LoginFailed", "User", user.Id,
                    $"محاولة دخون فاشلة — المحاولة {user.LoginAttempts}", null, ct);
                return Result<LoginResponse>.Failure("بيانات الاعتماد غير صالحة", ErrorCodes.Unauthorized);
            }

            // 7. Check if password change is required
            if (user.MustChangePassword)
            {
                _logger.LogInformation("Login requires password change for user: {UserName}", request.UserName);
                await _auditLogService.LogAsync(user.Id, "LoginRequiresPasswordChange", "User", user.Id,
                    "تسجيل الدخول يتطلب تغيير كلمة المرور", null, ct);

                // Return a token anyway so the client can navigate to password change screen
                string token = _jwtTokenGenerator.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

                return Result<LoginResponse>.Success(new LoginResponse(
                    user.Id, user.UserName, user.FullName, (byte)user.Role,
                    token, expiresAt, MustChangePassword: true));
            }

            // 8. Generate JWT
            string jwtToken = _jwtTokenGenerator.GenerateToken(user);
            var jwtExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

            _logger.LogInformation("Login successful for user: {UserName} (Id={UserId})", user.UserName, user.Id);
            await _auditLogService.LogAsync(user.Id, "LoginSuccess", "User", user.Id,
                "تسجيل دخول ناجح", null, ct);

            // 9. Return success
            return Result<LoginResponse>.Success(new LoginResponse(
                user.Id, user.UserName, user.FullName, (byte)user.Role,
                jwtToken, jwtExpiresAt, MustChangePassword: false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for user: {UserName}", request.UserName);
            return Result<LoginResponse>.Failure("حدث خطأ أثناء تسجيل الدخول.");
        }
    }

    public async Task<Result> SetPasswordAsync(SetPasswordRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null)
            {
                _logger.LogWarning("SetPassword failed: User {UserId} not found", userId);
                return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);
            }

            if (request.Password != request.ConfirmPassword)
            {
                return Result.Failure("كلمة المرور وتأكيدها غير متطابقين", ErrorCodes.ValidationError);
            }

            if (request.Password.Length < 6)
            {
                return Result.Failure("كلمة المرور يجب أن تكون 6 أحرف على الأقل", ErrorCodes.ValidationError);
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
            user.SetInitialPassword(passwordHash);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Password set for user {UserId}", userId);
            await _auditLogService.LogAsync(userId, "PasswordSet", "User", userId,
                "تم تعيين كلمة المرور الأولية", null, ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting password for user {UserId}", userId);
            return Result.Failure("حدث خطأ أثناء تعيين كلمة المرور.");
        }
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null)
            {
                _logger.LogWarning("ChangePassword failed: User {UserId} not found", userId);
                return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقين", ErrorCodes.ValidationError);
            }

            if (request.NewPassword.Length < 6)
            {
                return Result.Failure("كلمة المرور يجب أن تكون 6 أحرف على الأقل", ErrorCodes.ValidationError);
            }

            // Verify current password
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Result.Failure("يجب تعيين كلمة المرور الأولية أولاً", ErrorCodes.RequiresPasswordSetup);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("ChangePassword failed: Incorrect current password for user {UserId}", userId);
                await _auditLogService.LogAsync(userId, "PasswordChangeFailed", "User", userId,
                    "محاولة تغيير كلمة المرور — كلمة المرور الحالية غير صحيحة", null, ct);
                return Result.Failure("كلمة المرور الحالية غير صحيحة", ErrorCodes.Unauthorized);
            }

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
            user.ChangePassword(newPasswordHash);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Password changed for user {UserId}", userId);
            await _auditLogService.LogAsync(userId, "PasswordChanged", "User", userId,
                "تم تغيير كلمة المرور بنجاح", null, ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result.Failure("حدث خطأ أثناء تغيير كلمة المرور.");
        }
    }
}
