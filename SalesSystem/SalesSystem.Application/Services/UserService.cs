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

public class UserService : IUserService
{
    /// <summary>
    /// Default password assigned to new users and used for admin-initiated resets.
    /// </summary>
    private const string DefaultPassword = "12345678";

    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork uow,
        IPermissionService permissionService,
        IAuditLogService auditLogService,
        ILogger<UserService> logger)
    {
        _uow = uow;
        _permissionService = permissionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null)
            return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result<UserDto>.Failure("اسم المستخدم مطلوب", ErrorCodes.ValidationError);

        var user = await _uow.Users.FirstOrDefaultIgnoreFiltersAsync(
            u => u.UserName == userName, ct);
        if (user == null)
            return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        List<User> users;
        if (includeInactive)
        {
            users = await _uow.Users.ToListIgnoreFiltersAsync(ct);
        }
        else
        {
            users = await _uow.Users.ToListAsync(ct);
        }

        var dtos = users.Select(MapToDto).OrderByDescending(x => x.Id).ToList();
        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            // 1. Check duplicate username — use AnyAsync instead of loading all users
            var duplicateExists = await _uow.Users.AnyIgnoreFiltersAsync(
                u => u.UserName.ToLower() == request.UserName.ToLower().Trim(), ct);
            if (duplicateExists)
            {
                return Result<UserDto>.Failure("اسم المستخدم موجود بالفعل", ErrorCodes.DuplicateEntry);
            }

            // 2. Create entity with default password — user must change on first login.
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 12);
            var user = User.CreateWithPassword(
                request.UserName,
                defaultPasswordHash,
                request.FullName,
                phone: request.Phone,
                email: request.Email,
                employeeId: null,
                createdByUserId: null,
                mustChangePassword: true
            );

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. Assign role via UserRole join entity (many-to-many)
            var roleEntity = await _uow.Roles.GetByIdAsync(request.Role, ct);
            if (roleEntity != null)
            {
                var userRole = SalesSystem.Domain.Entities.UserRole.Create(user.Id, roleEntity.Id);
                await _uow.UserRoles.AddAsync(userRole, ct);
                await _uow.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Created new user: {UserName} (Id={UserId})", user.UserName, user.Id);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user");
            return Result<UserDto>.Failure("حدث خطأ أثناء إضافة المستخدم.");
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultIgnoreFiltersAsync(u => u.Id == id, ct);
            if (user == null)
                return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Update basic info — role is assigned separately via UserRole join entity
            user.Update(
                request.FullName,
                phone: request.Phone,
                email: request.Email,
                employeeId: null,
                updatedByUserId: null
            );

            // Update status
            var status = (UserStatus)request.Status;
            if (status == UserStatus.Active) user.Restore();
            else if (status == UserStatus.Inactive) user.MarkAsDeleted();
            else if (status == UserStatus.Locked) user.Lock();

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
                user.ChangePassword(passwordHash);
            }

            // Update role assignment via UserRole join entity
            var existingRoles = await _uow.UserRoles.ToListAsync(ur => ur.UserId == user.Id, ct: ct);
            _uow.UserRoles.DeleteRange(existingRoles);
            var newRole = await _uow.Roles.GetByIdAsync(request.Role, ct);
            if (newRole != null)
            {
                var userRole = SalesSystem.Domain.Entities.UserRole.Create(user.Id, newRole.Id);
                await _uow.UserRoles.AddAsync(userRole, ct);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Updated user: {UserName} (Id={UserId})", user.UserName, user.Id);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (DomainException ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user {Id}", id);
            return Result<UserDto>.Failure("حدث خطأ أثناء تحديث بيانات المستخدم.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(id, ct);
            if (user == null)
                return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Check if user has the Admin role (Id=1)
            var adminRoleId = 1;
            var isAdmin = await _uow.UserRoles.AnyAsync(
                ur => ur.UserId == user.Id && ur.RoleId == adminRoleId, ct);

            if (isAdmin && user.Status == UserStatus.Active)
            {
                var adminUserRoles = await _uow.UserRoles.ToListAsync(
                    ur => ur.RoleId == adminRoleId,
                    ct: ct,
                    includePaths: "User");
                var activeAdminCount = adminUserRoles
                    .Where(ur => ur.User?.Status == UserStatus.Active)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .Count();

                if (activeAdminCount <= 1)
                {
                    return Result.Failure("لا يمكن تعطيل آخر مدير نشط في النظام", ErrorCodes.InvalidOperation);
                }
            }

            await _uow.Users.SoftDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Deactivated user: {UserName} (Id={UserId})", user.UserName, user.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {Id}", id);
            return Result.Failure("حدث خطأ أثناء تعطيل المستخدم.");
        }
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        _logger.LogWarning("Attempt to hard-delete user {UserId} blocked — soft delete only", id);
        return Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي — استخدم خاصية تعطيل الحساب بدلاً من ذلك",
            ErrorCodes.InvalidOperation);
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(id, ct);
            if (user == null)
                return Result<ResetPasswordResponse>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Hash the default password and reset the user's password
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 12);
            user.ResetPassword(defaultPasswordHash);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Password reset for user {UserName} (Id={UserId}) to default", user.UserName, user.Id);
            await _auditLogService.LogAsync(null, "AdminForcePasswordChange", "User", user.Id,
                $"تم إعادة تعيين كلمة المرور للمستخدم {user.UserName} إلى الافتراضية من قبل المسؤول", null, ct);

            return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse(
                user.Id, "تم إعادة تعيين كلمة المرور إلى 12345678"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {Id}", id);
            return Result<ResetPasswordResponse>.Failure("حدث خطأ أثناء إعادة تعيين كلمة المرور.");
        }
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(id, ct);
            if (user == null)
                return Result<CurrentUserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            var permissionsResult = await _permissionService.GetUserPermissionsAsync(id, ct);
            var permissions = permissionsResult.IsSuccess ? permissionsResult.Value! : new List<string>();

            return Result<CurrentUserDto>.Success(new CurrentUserDto(
                Id: user.Id,
                UserName: user.UserName,
                FullName: user.FullName,
                Role: (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
                AvatarPath: null,  // Removed from User entity in v4.7+
                Permissions: permissions
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user data for {Id}", id);
            return Result<CurrentUserDto>.Failure("حدث خطأ أثناء تحميل بيانات المستخدم.");
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            UserName: user.UserName,
            FullName: user.FullName,
            Role: (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
            Status: (byte)user.Status,
            MustChangePassword: user.MustChangePassword,
            PasswordChangedAt: user.PasswordChangedAt,
            Phone: user.Phone,
            Email: user.Email,
            AvatarPath: null,        // Removed from User entity in v4.7+
            LastLoginAt: user.LastLoginAt,
            LoginAttempts: user.LoginAttempts,
            DefaultCashBoxId: null   // Removed from User entity in v4.7+
        );
    }
}
