using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IStoreSettingsService _settingsService;
    private readonly IPrintDataService _printSettingsService;

    public SettingsController(
        IStoreSettingsService settingsService,
        IPrintDataService printSettingsService)
    {
        _settingsService = settingsService;
        _printSettingsService = printSettingsService;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _settingsService.GetSettingsAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            return Ok(result.Value);
        }
        if (result.IsSuccess)
            return Ok(result.Value);
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var result = await _settingsService.UpdateSettingsAsync(request, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    // ─── System Settings Bulk Endpoints ──────

    /// <summary>
    /// GET /api/v1/settings/system — returns all SystemSettings as a flat key-value dictionary.
    /// Used by the new SystemSettings UI screen.
    /// </summary>
    [HttpGet("system")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllSystemSettings(CancellationToken ct)
    {
        var result = await _settingsService.GetAllSystemSettingsAsync(ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// PUT /api/v1/settings/system — batch update of SystemSettings key-value pairs.
    /// </summary>
    [HttpPut("system")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateSystemSettings([FromBody] Dictionary<string, string> settings, CancellationToken ct)
    {
        if (settings == null || settings.Count == 0)
            return BadRequest(new { error = "لا توجد إعدادات للتحديث" });

        var result = await _settingsService.UpdateSystemSettingsAsync(settings, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم حفظ إعدادات النظام بنجاح" });
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    // ─── Print Settings Endpoints ────────────

    [HttpGet("print")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetPrintSettings(CancellationToken ct)
    {
        var result = await _printSettingsService.GetPrintSettingsAsync(ct);
        if (result.IsSuccess && result.Value != null)
            return Ok(result.Value);
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpPut("print")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> UpdatePrintSettings([FromBody] UpdatePrintSettingsRequest request, CancellationToken ct)
    {
        var result = await _printSettingsService.UpdatePrintSettingsAsync(request, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم حفظ إعدادات الطباعة بنجاح" });
        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }
}
