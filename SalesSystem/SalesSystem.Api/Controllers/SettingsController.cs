using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Contracts.DTOs;
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
    private readonly ISystemSettingsRepository _systemSettings;

    public SettingsController(IStoreSettingsService settingsService, ISystemSettingsRepository systemSettings)
    {
        _settingsService = settingsService;
        _systemSettings = systemSettings;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _settingsService.GetSettingsAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            var costingMethod = await _systemSettings.GetCostingMethodAsync(ct);
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
            await _systemSettings.SetCostingMethodAsync((CostingMethod)request.CostingMethod, ct);
            return Ok(result.Value);
        }
        return BadRequest(new { error = result.Error });
    }

    // ─── Print Settings Endpoints ────────────

    [HttpGet("print")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPrintSettings(CancellationToken ct)
    {
        var thermalPrinter = await _systemSettings.GetStringAsync("ThermalPrinterName", "", ct);
        var a4Printer = await _systemSettings.GetStringAsync("A4PrinterName", "", ct);
        var logoPath = await _systemSettings.GetStringAsync("LogoPath", "", ct);
        var storeTaxNumber = await _systemSettings.GetStringAsync("StoreTaxNumber", "", ct);
        var taxRateStr = await _systemSettings.GetStringAsync("TaxRate", "15", ct);
        decimal.TryParse(taxRateStr, out var taxRate);

        var dto = new PrintSettingsDto(
            thermalPrinter ?? "",
            a4Printer ?? "",
            logoPath ?? "",
            storeTaxNumber ?? "",
            taxRate);

        return Ok(dto);
    }

    [HttpPut("print")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePrintSettings([FromBody] UpdatePrintSettingsRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var uid) ? uid : null;

        await _systemSettings.SetStringAsync("ThermalPrinterName", request.ThermalPrinterName ?? "", userId, ct);
        await _systemSettings.SetStringAsync("A4PrinterName", request.A4PrinterName ?? "", userId, ct);
        await _systemSettings.SetStringAsync("LogoPath", request.LogoPath ?? "", userId, ct);
        await _systemSettings.SetStringAsync("StoreTaxNumber", request.StoreTaxNumber ?? "", userId, ct);
        await _systemSettings.SetStringAsync("TaxRate", request.TaxRate.ToString(), userId, ct);

        return Ok(new { message = "تم حفظ إعدادات الطباعة بنجاح" });
    }
}
