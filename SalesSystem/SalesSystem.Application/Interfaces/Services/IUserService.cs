using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct);
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);

    /// <summary>
    /// Gets the current user's profile with permissions.
    /// </summary>
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Resets a user's password (admin function) — generates a one-time reset token,
    /// clears the password hash, and sets MustChangePassword.
    /// Returns the reset token that must be shared with the user.
    /// </summary>
    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(int id, CancellationToken ct);
}
