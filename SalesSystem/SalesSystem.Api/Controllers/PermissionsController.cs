using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for bitmask-based permission management.
/// Permissions are stored as a BIGINT bitmask on both User and Role entities.
/// Super Admin = PermissionsMask == -1 (all bits set — bypasses all checks).
/// </summary>
[ApiController]
[Route("api/v1/permissions")]
[Authorize(Policy = "AdminOnly")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Returns all known permission code strings.
    /// These are the canonical codes used in the bitmask mapping.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var codes = PermissionService.AllPermissionCodes.OrderBy(x => x).ToList();
        return Ok(codes);
    }

    /// <summary>
    /// Returns a dictionary mapping each role ID to its PermissionsMask value.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(Dictionary<short, long>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRoleMasks(CancellationToken ct)
    {
        var result = await _permissionService.GetAllRoleMasksAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Sets the PermissionsMask for a given role.
    /// The mask is a bitwise-OR of permission bit values.
    /// Use -1 for Super Admin (all permissions).
    /// Use 0 for no permissions.
    /// </summary>
    /// <param name="role">The role ID (short).</param>
    /// <param name="request">The mask value to set.</param>
    [HttpPut("roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRoleMask(short role, [FromBody] SetRoleMaskRequest request, CancellationToken ct)
    {
        var result = await _permissionService.SetRolePermissionsMaskAsync(role, request.Mask, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم تعيين صلاحيات الدور بنجاح" });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets the list of permission code strings for the current user.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "المستخدم غير موثّق" });

        var result = await _permissionService.GetUserPermissionsAsync(userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

/// <summary>
/// Request model for setting a role's permissions mask.
/// </summary>
public record SetRoleMaskRequest(long Mask);
