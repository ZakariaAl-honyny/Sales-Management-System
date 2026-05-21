using Microsoft.EntityFrameworkCore;
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
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork uow, ILogger<UserService> logger)
    {
        _uow = uow;
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
        var query = _uow.Users.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        var users = await query.ToListAsync(ct);
        var dtos = users.Select(MapToDto).ToList();
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

            // 2. Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

            // 3. Create entity
            var user = User.Create(
                request.UserName,
                passwordHash,
                request.FullName,
                (UserRole)request.Role
            );

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Created new user: {UserName}", user.UserName);

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
            var user = await _uow.Users.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null)
                return Result<UserDto>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Update basic info
            user.Update(request.FullName, (UserRole)request.Role);
            
            // Update status
            if (request.IsActive) user.Restore();
            else user.MarkAsDeleted();

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
                user.ChangePassword(passwordHash);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Updated user: {UserName}", user.UserName);

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
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        if (user.Role == UserRole.Admin && user.IsActive)
        {
            var users = await _uow.Users.GetAllAsync(ct);
            var activeAdmins = users.Count(u => u.Role == UserRole.Admin && u.IsActive);
            if (activeAdmins <= 1)
            {
                return Result.Failure("لا يمكن تعطيل آخر مدير نشط في النظام", ErrorCodes.InvalidOperation);
            }
        }

        await _uow.Users.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Deactivated user: {UserName}", user.UserName);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var user = await _uow.Users.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

        if (user.Role == UserRole.Admin)
        {
            var users = await _uow.Users.Query().IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id)
                .ToListAsync(ct);
            if (users.Count == 0)
            {
                return Result.Failure("لا يمكن حذف آخر مدير في النظام", ErrorCodes.InvalidOperation);
            }
        }

        await _uow.Users.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User permanently deleted: {UserName}", user.UserName);
        return Result.Success();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(user.Id, user.UserName, user.FullName, (byte)user.Role, user.IsActive);
    }
}
