using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing user sessions.
/// Access restricted to Admin role only.
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
[Authorize(Policy = "AdminOnly")]
public class SessionsController : ControllerBase
{
    private readonly ISessionManagementService _sessionService;

    public SessionsController(ISessionManagementService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Gets all user sessions, optionally filtered by user and revoked status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Contracts.DTOs.UserSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? userId = null,
        [FromQuery] bool includeRevoked = false,
        CancellationToken ct = default)
    {
        var result = await _sessionService.GetAllAsync(userId, includeRevoked, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Revokes a specific user session.
    /// </summary>
    [HttpPost("{id:long}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(long id, CancellationToken ct)
    {
        var result = await _sessionService.RevokeAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم إلغاء الجلسة بنجاح" });
        if (result.ErrorCode == Contracts.Common.ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
