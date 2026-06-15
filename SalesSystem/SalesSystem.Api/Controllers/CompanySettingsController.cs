using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/company-settings")]
[Authorize(Policy = "AdminOnly")]
public class CompanySettingsController : ControllerBase
{
    private readonly ICompanySettingsService _service;

    public CompanySettingsController(ICompanySettingsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets the current company settings.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _service.GetAsync(ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates the company settings (Admin only).
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCompanySettingsRequest request, CancellationToken ct)
    {
        // Extract userId from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? userId = userIdClaim != null && int.TryParse(userIdClaim, out var uid) ? uid : null;

        var result = await _service.UpdateAsync(request, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
