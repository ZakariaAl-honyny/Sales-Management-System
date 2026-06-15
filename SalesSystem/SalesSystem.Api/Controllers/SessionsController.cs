using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;

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
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(IUnitOfWork uow, ILogger<SessionsController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <summary>
    /// Gets all user sessions, optionally filtered by user and revoked status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? userId = null,
        [FromQuery] bool includeRevoked = false,
        CancellationToken ct = default)
    {
        List<UserSession> sessions;

        if (userId.HasValue)
        {
            if (includeRevoked)
                sessions = await _uow.UserSessions.ToListAsync(
                    s => s.UserId == userId.Value, null, ct, ignoreQueryFilters: true);
            else
                sessions = await _uow.UserSessions.ToListAsync(
                    s => s.UserId == userId.Value, ct: ct);
        }
        else
        {
            if (includeRevoked)
                sessions = await _uow.UserSessions.ToListIgnoreFiltersAsync(ct: ct);
            else
                sessions = await _uow.UserSessions.ToListAsync(
                    s => !s.IsRevoked, ct: ct);
        }

        var dtos = sessions.Select(MapToDto).OrderByDescending(x => x.CreatedAt).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Revokes a specific user session.
    /// </summary>
    [HttpPost("{id:long}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(long id, CancellationToken ct)
    {
        var session = await _uow.UserSessions.GetByIdAsync((int)id, ct);
        if (session == null)
            return NotFound(new { error = "الجلسة غير موجودة" });

        session.Revoke();
        await _uow.UserSessions.UpdateAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Session {SessionId} revoked by admin", id);
        return Ok(new { message = "تم إلغاء الجلسة بنجاح" });
    }

    private static UserSessionDto MapToDto(UserSession s)
    {
        return new UserSessionDto(
            Id: s.Id,
            UserId: s.UserId,
            UserName: s.User?.UserName,
            FullName: s.User?.FullName ?? "",
            DeviceName: s.DeviceName,
            IpAddress: s.IpAddress,
            CreatedAt: s.CreatedAt,
            LastActivityAt: s.LastActivityAt,
            ExpiresAt: s.ExpiresAt,
            IsRevoked: s.IsRevoked);
    }
}
