using System.Security.Cryptography;
using System.Text;
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

            // 1. Find user — query with predicate instead of loading all users into memory
            var user = await _uow.Users.FirstOrDefaultAsync(
                u => u.UserName.ToLower() == request.UserName.ToLower().Trim(), ct);

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
                    "محاولة دخول لحساب مقفول", null, ct, autoSave: false);
                await _uow.SaveChangesAsync(ct);
                return Result<LoginResponse>.Failure("الحساب مقفول — يرجى الاتصال بالمسؤول", ErrorCodes.AccountLocked);
            }

            // 3. Check if account is active (not inactive/soft-deleted)
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Login failed: User {UserName} is inactive", request.UserName);
                return Result<LoginResponse>.Failure("الحساب معطل", ErrorCodes.Forbidden);
            }

            // 4. Check if password hash is null (edge case from old seed data)
            // With the default-password flow, new users always have a password set.
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _logger.LogWarning("Login failed: User {UserName} has no password set — requires password setup", request.UserName);
                await _auditLogService.LogAsync(user.Id, "LoginRequiresPasswordSetup", "User", user.Id,
                    "تسجيل الدخول يتطلب تعيين كلمة المرور الأولية", null, ct, autoSave: false);
                await _uow.SaveChangesAsync(ct);
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
                    $"محاولة دخول فاشلة — المحاولة {user.LoginAttempts}", null, ct, autoSave: false);
                await _uow.SaveChangesAsync(ct);
                return Result<LoginResponse>.Failure("بيانات الاعتماد غير صالحة", ErrorCodes.Unauthorized);
            }

            // 7. Check if password change is required
            if (user.MustChangePassword)
            {
                _logger.LogInformation("Login requires password change for user: {UserName}", request.UserName);
                await _auditLogService.LogAsync(user.Id, "LoginRequiresPasswordChange", "User", user.Id,
                    "تسجيل الدخول يتطلب تغيير كلمة المرور", null, ct, autoSave: false);

                // Return a token anyway so the client can navigate to password change screen
                string token = _jwtTokenGenerator.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

                // Create session
                var session = UserSession.Create(user.Id, HashToken(token), DateTime.UtcNow, _jwtSettings.ExpirationHours);
                await _uow.UserSessions.AddAsync(session, ct);

                await _uow.SaveChangesAsync(ct);

                return Result<LoginResponse>.Success(new LoginResponse(
                    user.Id, user.UserName, user.FullName, (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
                    token, expiresAt, MustChangePassword: true));
            }

            // 8. Generate JWT
            string jwtToken = _jwtTokenGenerator.GenerateToken(user);
            var jwtExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

            // Create session
            var userSession = UserSession.Create(user.Id, HashToken(jwtToken), DateTime.UtcNow, _jwtSettings.ExpirationHours);
            await _uow.UserSessions.AddAsync(userSession, ct);

            _logger.LogInformation("Login successful for user: {UserName} (Id={UserId})", user.UserName, user.Id);
            await _auditLogService.LogAsync(user.Id, "LoginSuccess", "User", user.Id,
                "تسجيل دخول ناجح", null, ct, autoSave: false);

            // 9. Persist all changes (user session + audit log) atomically
            await _uow.SaveChangesAsync(ct);

            // 10. Return success
            return Result<LoginResponse>.Success(new LoginResponse(
                user.Id, user.UserName, user.FullName, (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
                jwtToken, jwtExpiresAt, MustChangePassword: false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for user: {UserName}", request.UserName);
            return Result<LoginResponse>.Failure("حدث خطأ أثناء تسجيل الدخول.");
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
                    "محاولة تغيير كلمة المرور — كلمة المرور الحالية غير صحيحة", null, ct, autoSave: false);
                await _uow.SaveChangesAsync(ct);
                return Result.Failure("كلمة المرور الحالية غير صحيحة", ErrorCodes.Unauthorized);
            }

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
            user.ChangePassword(newPasswordHash);

            _logger.LogInformation("Password changed for user {UserId}", userId);
            await _auditLogService.LogAsync(userId, "PasswordChanged", "User", userId,
                "تم تغيير كلمة المرور بنجاح", null, ct, autoSave: false);

            // Persist all changes (user state + audit log) atomically
            await _uow.SaveChangesAsync(ct);

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

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
