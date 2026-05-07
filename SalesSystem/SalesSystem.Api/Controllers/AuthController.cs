using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Auth;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Login response with token if successful.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return Unauthorized(new { error = result.Error });
    }
}
