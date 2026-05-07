using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for generating JWT tokens for authenticated users.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="user">The user to generate the token for.</param>
    /// <returns>A JWT token string.</returns>
    string GenerateToken(User user);
}