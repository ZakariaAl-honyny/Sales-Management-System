using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork uow,
        IPermissionService permissionService,
        ILogger<UserService> logger)
    {
        _uow = uow;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
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
            // 1. Check duplicate username
            var existingUsers = await _uow.Users.GetAllAsync(ct);
            if (existingUsers.Any(u => u.UserName.Equals(request.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<UserDto>.Failure("اسم المستخدم موجود بالفعل", ErrorCodes.DuplicateEntry);
            }

            // 2. Create entity — passwordless! User sets password via SetInitialPassword.
            var user = User.Create(
                request.UserName,
                request.FullName,
                (UserRole)request.Role,
                request.Phone,
                request.Email,
                request.DefaultCashBoxId
            );

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

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

            // Update basic info with new fields
            user.Update(
                request.FullName,
                (UserRole)request.Role,
                request.Phone,
                request.Email,
                request.DefaultCashBoxId
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

            if (user.Role == UserRole.Admin && user.Status == UserStatus.Active)
            {
                var users = await _uow.Users.GetAllAsync(ct);
                var activeAdmins = users.Count(u => u.Role == UserRole.Admin && u.Status == UserStatus.Active);
                if (activeAdmins <= 1)
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

    public async Task<Result> ResetPasswordAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(id, ct);
            if (user == null)
                return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            user.ResetPassword();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Password reset for user {UserName} (Id={UserId})", user.UserName, user.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {Id}", id);
            return Result.Failure("حدث خطأ أثناء إعادة تعيين كلمة المرور.");
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
                Role: (byte)user.Role,
                AvatarPath: user.AvatarPath,
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
            Role: (byte)user.Role,
            Status: (byte)user.Status,
            MustChangePassword: user.MustChangePassword,
            PasswordChangedAt: user.PasswordChangedAt,
            Phone: user.Phone,
            Email: user.Email,
            AvatarPath: user.AvatarPath,
            LastLoginAt: user.LastLoginAt,
            LoginAttempts: user.LoginAttempts,
            DefaultCashBoxId: user.DefaultCashBoxId
        );
    }
}
