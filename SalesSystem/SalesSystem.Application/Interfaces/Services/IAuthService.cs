using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests.Auth;
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
}
