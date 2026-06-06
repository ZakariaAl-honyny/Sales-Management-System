using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/currencies")]
[Authorize(Policy = "AdminOnly")]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CurrencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _currencyService.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _currencyService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request, CancellationToken ct)
    {
        // Default userId = 1 until real auth context is provided
        var userId = GetUserId();
        var result = await _currencyService.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCurrencyRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _currencyService.UpdateAsync(id, request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _currencyService.DeleteAsync(id, userId, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("permanent/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _currencyService.DeletePermanentlyAsync(id, userId, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف النهائي بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}/exchange-rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateExchangeRate(int id, [FromBody] UpdateExchangeRateRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _currencyService.UpdateExchangeRateAsync(id, request.NewRate, userId, ct);
        return result.IsSuccess ? Ok(new { message = "تم تحديث سعر الصرف بنجاح" }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(List<ExchangeRateHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRateHistory(int id, CancellationToken ct)
    {
        var result = await _currencyService.GetRateHistoryAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Extracts user ID from JWT claims, or falls back to 1 for system operations.
    /// </summary>
    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return 1; // Default admin fallback
    }
}
