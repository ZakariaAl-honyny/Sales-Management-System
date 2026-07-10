using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;

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
        var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == id, ct, "UserRoles");
        if (user == null)
            return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result<UserDto>.Failure("اسم المستخدم مطلوب", ErrorCodes.ValidationError);

        var user = await _uow.Users.FirstOrDefaultIgnoreFiltersAsync(
            u => u.UserName == userName, ct, "UserRoles");
        if (user == null)
            return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        List<User> users;
        if (includeInactive)
        {
            users = await _uow.Users.ToListIgnoreFiltersAsync(ct, "UserRoles");
        }
        else
        {
            users = await _uow.Users.ToListAsync(ct, "UserRoles");
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

            // 2. Hash the provided password or fall back to the default password.
            //    User must change password on first login when using the default.
            string passwordToHash = string.IsNullOrWhiteSpace(request.Password) ? DefaultPassword : request.Password;
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash, workFactor: 12);
            var user = User.CreateWithPassword(
                request.UserName,
                passwordHash,
                employeeId: null,
                createdByUserId: null,
                mustChangePassword: true
            );

            // 3. Atomic: save user + assign roles in single transaction
            await _uow.ExecuteTransactionAsync(async () =>
            {
                await _uow.Users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                // Assign roles and copy PermissionsMask
                int assignedRoleId = 0;
                if (request.RoleIds?.Any() == true)
                {
                    assignedRoleId = request.RoleIds.First();
                    foreach (var roleId in request.RoleIds)
                    {
                        var userRole = SalesSystem.Domain.Entities.UserRole.Create(user.Id, (short)roleId);
                        await _uow.UserRoles.AddAsync(userRole, ct);
                    }
                }
                else if (request.Role > 0)
                {
                    assignedRoleId = request.Role;
                    var userRole = SalesSystem.Domain.Entities.UserRole.Create(user.Id, (short)request.Role);
                    await _uow.UserRoles.AddAsync(userRole, ct);
                }

                // Copy role's PermissionsMask to user
                if (assignedRoleId > 0)
                {
                    var assignedRole = await _uow.Roles.GetByIdAsync(assignedRoleId, ct);
                    if (assignedRole != null)
                        user.SetPermissionsMask(assignedRole.PermissionsMask);
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

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
            var user = await _uow.Users.FirstOrDefaultIgnoreFiltersAsync(u => u.Id == id, ct, "UserRoles");
            if (user == null)
                return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Update basic info — role is assigned separately via UserRole join entity
            user.Update(
                employeeId: null,
                updatedByUserId: null
            );

            // Update lock/active state
            if (request.IsLocked.HasValue)
            {
                if (request.IsLocked.Value) user.Lock();
                else user.Unlock();
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
                user.ChangePassword(passwordHash);
            }

            // Update role assignment via UserRole join entity + copy PermissionsMask
            var existingRoles = await _uow.UserRoles.ToListAsync(ur => ur.UserId == user.Id, ct: ct);
            _uow.UserRoles.DeleteRange(existingRoles);
            var newRole = await _uow.Roles.GetByIdAsync(request.Role, ct);
            if (newRole != null)
            {
                var userRole = SalesSystem.Domain.Entities.UserRole.Create(user.Id, newRole.Id);
                await _uow.UserRoles.AddAsync(userRole, ct);
                // Copy role's PermissionsMask to user
                user.SetPermissionsMask(newRole.PermissionsMask);
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

            if (isAdmin && user.IsActive)
            {
                var adminUserRoles = await _uow.UserRoles.ToListAsync(
                    ur => ur.RoleId == adminRoleId,
                    ct: ct,
                    includePaths: "User");
                var activeAdminCount = adminUserRoles
                    .Where(ur => ur.User != null && ur.User.IsActive)
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
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == id, ct, "UserRoles");
            if (user == null)
                return Result<CurrentUserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            var permissionsResult = await _permissionService.GetUserPermissionsAsync(id, ct);
            var permissions = permissionsResult.IsSuccess ? permissionsResult.Value! : new List<string>();

            return Result<CurrentUserDto>.Success(new CurrentUserDto(
                Id: user.Id,
                UserName: user.UserName,
                Role: (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
                AvatarPath: null,
                Permissions: permissions
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user data for {Id}", id);
            return Result<CurrentUserDto>.Failure("حدث خطأ أثناء تحميل بيانات المستخدم.");
        }
    }

    // ─── Role Management ────────────────────────────

    public async Task<Result<List<UserRoleDto>>> GetUserRolesAsync(int userId, CancellationToken ct)
    {
        try
        {
            var userRoles = await _uow.UserRoles.ToListAsync(
                ur => ur.UserId == userId, null, ct, ignoreQueryFilters: true, "Role");

            var dtos = userRoles.Select(ur => new UserRoleDto(
                ur.UserId, ur.RoleId, ur.Role?.Name)).ToList();

            return Result<List<UserRoleDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return Result<List<UserRoleDto>>.Failure("حدث خطأ أثناء جلب أدوار المستخدم.");
        }
    }

    public async Task<Result> UpdateUserRolesAsync(int userId, List<int> roleIds, CancellationToken ct)
    {
        try
        {
            var existingRoles = await _uow.UserRoles.ToListAsync(
                ur => ur.UserId == userId, null, ct, ignoreQueryFilters: true);
            _uow.UserRoles.DeleteRange(existingRoles);

            foreach (var roleId in roleIds)
            {
                var userRole = SalesSystem.Domain.Entities.UserRole.Create(userId, (short)roleId);
                await _uow.UserRoles.AddAsync(userRole, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
            return Result.Failure("حدث خطأ أثناء تحديث أدوار المستخدم.");
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            UserName: user.UserName,
            Role: (byte)(user.UserRoles.FirstOrDefault()?.RoleId ?? 0),
            MustChangePassword: user.MustChangePassword,
            IsLocked: user.IsLocked,
            IsActive: user.IsActive,
            AvatarPath: null,
            LastLoginAt: user.LastLoginAt,
            LoginAttempts: user.LoginAttempts,
            DefaultCashBoxId: null
        );
    }
}
