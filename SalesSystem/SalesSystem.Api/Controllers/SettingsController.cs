using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Store settings management API
/// </summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IStoreSettingsService _settingsService;

    public SettingsController(IStoreSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Gets current store settings
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Store settings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(StoreSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _settingsService.GetSettingsAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates store settings
    /// </summary>
    /// <param name="request">Settings update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated settings</returns>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(StoreSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _settingsService.UpdateSettingsAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
