using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the login response with user info and token.</returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sets the initial password for a passwordless user (first login flow).
    /// </summary>
    /// <param name="request">The set password request with new password and confirmation.</param>
    /// <param name="userId">The user ID to set the password for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetPasswordAsync(SetPasswordRequest request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Changes the password for an authenticated user (requires current password).
    /// </summary>
    /// <param name="request">The change password request with current, new, and confirm passwords.</param>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, int userId, CancellationToken ct = default);
}
