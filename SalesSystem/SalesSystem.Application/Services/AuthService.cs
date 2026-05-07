using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests.Auth;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Implementation of the authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork uow,
        IJwtTokenGenerator jwtTokenGenerator,
        JwtSettings jwtSettings,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for user: {UserName}", request.UserName);

        // 1. Find user (Using GetAllAsync and filtering as per task T010 requirement)
        var users = await _uow.Users.GetAllAsync(ct);
        var user = users.FirstOrDefault(u => u.UserName.Equals(request.UserName, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {UserName} not found", request.UserName);
            return Result<LoginResponse>.Failure("Invalid credentials", ErrorCodes.Unauthorized);
        }

        // 2. Check if active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {UserName} is inactive", request.UserName);
            return Result<LoginResponse>.Failure("Account is disabled", ErrorCodes.Forbidden);
        }

        // 3. Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Incorrect password for user {UserName}", request.UserName);
            return Result<LoginResponse>.Failure("Invalid credentials", ErrorCodes.Unauthorized);
        }

        // 4. Generate JWT
        string token = _jwtTokenGenerator.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

        _logger.LogInformation("Login successful for user: {UserName}", request.UserName);

        // 5. Return success result
        return Result<LoginResponse>.Success(new LoginResponse(
            user.Id,
            user.UserName,
            user.FullName,
            (byte)user.Role,
            token,
            expiresAt
        ));
    }
}
