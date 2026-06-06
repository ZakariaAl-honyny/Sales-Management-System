using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing permissions and role-permission assignments.
/// Access restricted to Admin role only (Policy: AdminOnly).
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
    /// Returns all active permissions, ordered by Category then Name.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns list of all active permissions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _permissionService.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns a dictionary mapping each Role to the list of assigned permission IDs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns role-permission mappings.</returns>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(Dictionary<UserRole, List<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRolePermissions(CancellationToken ct)
    {
        var result = await _permissionService.GetRolePermissionsAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates the permission set for a given role.
    /// Replaces all existing role permissions with the new set.
    /// </summary>
    /// <param name="role">The role ID (1=Admin, 2=Manager, 3=Cashier).</param>
    /// <param name="permissionIds">List of permission IDs to assign to the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message.</returns>
    [HttpPut("roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRolePermissions(byte role, [FromBody] List<int> permissionIds, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            return BadRequest(new { error = "دور غير صالح. يجب أن يكون 1 (مدير النظام), 2 (مدير), أو 3 (كاشير)" });

        var result = await _permissionService.UpdateRolePermissionsAsync((UserRole)role, permissionIds, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم تحديث الصلاحيات بنجاح" });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets the list of permission names for the current user based on their role.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns list of permission names for the current user.</returns>
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
