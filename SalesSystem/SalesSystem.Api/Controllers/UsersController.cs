using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing system users.
/// </summary>
/// <remarks>
/// Access restricted to Admin role only (Policy: AdminOnly) except for GetCurrentUser.
/// </remarks>
[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _uow;

    public UsersController(IUserService userService, IUnitOfWork uow)
    {
        _userService = userService;
        _uow = uow;
    }

    /// <summary>
    /// Retrieves all users.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns list of all users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _userService.GetAllAsync(includeInactive, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the user if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Gets the current authenticated user's profile with permissions.
    /// Accessible by all authenticated users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the current user's profile with permissions.</returns>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "المستخدم غير موثّق" });

        var result = await _userService.GetCurrentUserAsync(userId, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new user (passwordless — user must set password on first login).
    /// </summary>
    /// <param name="request">Create user request with UserName and Role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created user with ID.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">User ID to update.</param>
    /// <param name="request">Update user request with Role and optional fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated user.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deactivates a user (Soft Delete).
    /// </summary>
    /// <param name="id">User ID to deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deactivated ID.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _userService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم تعطيل المستخدم بنجاح", id });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Permanently deletes a user.
    /// </summary>
    /// <param name="id">User ID to delete permanently.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    /// <remarks>
    /// Deprecated — hard delete no longer allowed per RULE-038.
    /// The endpoint still exists but will return a failure response.
    /// Use DELETE api/v1/users/{id} (soft delete) instead.
    /// </remarks>
    [HttpDelete("permanent/{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _userService.PermanentDeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف النهائي بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Resets a user's password to the default "12345678" (admin function).
    /// Forces MustChangePassword = true so the user must change on next login.
    /// </summary>
    /// <param name="id">User ID to reset password for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message.</returns>
    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        var result = await _userService.ResetPasswordAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new
            {
                userId = result.Value!.UserId,
                message = result.Value.Message
            });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    // ═══════════════════════════════════════════════════════════
    // User Role Management
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    [HttpGet("{id:int}/roles")]
    [ProducesResponseType(typeof(List<UserRoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(int id, CancellationToken ct)
    {
        var userRoles = await _uow.UserRoles.ToListAsync(
            ur => ur.UserId == id, null, ct, ignoreQueryFilters: true, "Role");

        var dtos = userRoles.Select(ur => new UserRoleDto(
            ur.UserId, ur.RoleId, ur.Role?.Name)).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Updates the roles assigned to a user.
    /// </summary>
    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] List<int> roleIds, CancellationToken ct)
    {
        try
        {
            var existingRoles = await _uow.UserRoles.ToListAsync(
                ur => ur.UserId == id, null, ct, ignoreQueryFilters: true);
            _uow.UserRoles.DeleteRange(existingRoles);

            foreach (var roleId in roleIds)
            {
                var userRole = UserRole.Create(id, (short)roleId);
                await _uow.UserRoles.AddAsync(userRole, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Ok(new { message = "تم تحديث أدوار المستخدم بنجاح" });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error updating roles for user {UserId}", id);
            return BadRequest(new { error = "حدث خطأ أثناء تحديث أدوار المستخدم" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // User Branch Management
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the branches assigned to a user.
    /// </summary>
    [HttpGet("{id:int}/branches")]
    [ProducesResponseType(typeof(List<UserBranchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBranches(int id, CancellationToken ct)
    {
        var userBranches = await _uow.UserBranches.ToListAsync(
            ub => ub.UserId == id, null, ct, ignoreQueryFilters: true, "Branch");

        var dtos = userBranches.Select(ub => new UserBranchDto(
            ub.UserId, ub.BranchId, ub.Branch?.Name)).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Updates the branches assigned to a user.
    /// </summary>
    [HttpPut("{id:int}/branches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserBranches(int id, [FromBody] List<short> branchIds, CancellationToken ct)
    {
        try
        {
            var existingBranches = await _uow.UserBranches.ToListAsync(
                ub => ub.UserId == id, null, ct, ignoreQueryFilters: true);
            _uow.UserBranches.DeleteRange(existingBranches);

            foreach (var branchId in branchIds)
            {
                var userBranch = UserBranch.Create(id, branchId);
                await _uow.UserBranches.AddAsync(userBranch, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return Ok(new { message = "تم تحديث فروع المستخدم بنجاح" });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error updating branches for user {UserId}", id);
            return BadRequest(new { error = "حدث خطأ أثناء تحديث فروع المستخدم" });
        }
    }
}
