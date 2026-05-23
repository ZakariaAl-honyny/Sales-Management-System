using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
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

    public SettingsController(IStoreSettingsService settingsService)
    {
        _settingsService = settingsService;
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

        var method = (CostingMethod)request.Method;
        if (!Enum.IsDefined(typeof(CostingMethod), method))
            return BadRequest(new { error = "طريقة التكلفة غير صالحة" });

        var result = await _settingsService.SetCostingMethodAsync(method, userId, ct);
        if (result.IsSuccess)
            return Ok((int)method);
        return BadRequest(new { error = result.Error });
    }

    // ─── Print Settings Endpoints ────────────
    // Note: Print settings handled via dedicated PrintController endpoints.
}
