using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Enums;
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
            var costingResult = await _settingsService.GetCostingMethodAsync(ct);
            var costingMethod = costingResult.IsSuccess && costingResult.Value.HasValue ? costingResult.Value.Value : CostingMethod.WeightedAverage;
            var dto = result.Value with { CostingMethod = (int)costingMethod };
            return Ok(dto);
        }
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var result = await _settingsService.UpdateSettingsAsync(request, userId, ct);
        if (result.IsSuccess)
        {
            var costingResult = await _settingsService.SetCostingMethodAsync((CostingMethod)request.CostingMethod, userId, ct);
            if (!costingResult.IsSuccess)
                return Ok(new { warning = costingResult.Error, settings = result.Value });
            return Ok(result.Value);
        }
        return BadRequest(new { error = result.Error });
    }

    // ─── Costing Method Endpoints ────────────

    [HttpGet("costing-method")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetCostingMethod(CancellationToken ct)
    {
        var result = await _settingsService.GetCostingMethodAsync(ct);
        if (result.IsSuccess)
        {
            var method = result.Value ?? CostingMethod.WeightedAverage;
            return Ok((int)method);
        }
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("costing-method")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> UpdateCostingMethod([FromBody] UpdateCostingMethodRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _settingsService.SetCostingMethodAsync((CostingMethod)request.Method, userId, ct);
        if (result.IsSuccess)
            return Ok(new { method = (int)request.Method });
        return BadRequest(new { error = result.Error });
    }

    // ─── Print Settings Endpoints ────────────

    [HttpGet("print")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetPrintSettings(CancellationToken ct)
    {
        var result = await _printSettingsService.GetPrintSettingsAsync(ct);
        if (result.IsSuccess && result.Value != null)
            return Ok(result.Value);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("print")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> UpdatePrintSettings([FromBody] UpdatePrintSettingsRequest request, CancellationToken ct)
    {
        var result = await _printSettingsService.UpdatePrintSettingsAsync(request, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم حفظ إعدادات الطباعة بنجاح" });
        return BadRequest(new { error = result.Error });
    }
}
