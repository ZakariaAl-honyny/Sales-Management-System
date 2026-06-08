using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/cash-boxes")]
[Authorize]
public class CashBoxesController : ControllerBase
{
    private readonly ICashBoxService _service;

    public CashBoxesController(ICashBoxService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Create([FromBody] CreateCashBoxRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCashBoxRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}/transactions")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetTransactions(int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await _service.GetTransactionsAsync(id, from, to, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/transactions")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> RecordExpense(int id, [FromBody] AddCashTransactionRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.RecordExpenseAsync(id, request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("transfer")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Transfer([FromBody] CashTransferRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.TransferAsync(request, userId, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}/daily-closures")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetDailyClosures(int id, CancellationToken ct)
    {
        var result = await _service.GetDailyClosuresAsync(id, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/daily-closures")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PerformDailyClosure(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.PerformDailyClosureAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
