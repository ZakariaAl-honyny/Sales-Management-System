using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Inventory write-off operations (الإتلاف)
/// </summary>
[ApiController]
[Route("api/v1/inventory")]
[Authorize(Policy = "ManagerAndAbove")]
public class InventoryWriteOffController : ControllerBase
{
    private readonly IInventoryWriteOffService _writeOffService;

    public InventoryWriteOffController(IInventoryWriteOffService writeOffService)
    {
        _writeOffService = writeOffService;
    }

    /// <summary>
    /// Writes off expired or damaged stock from inventory.
    /// Creates a StockWriteOff record, decreases warehouse stock, and logs an inventory movement.
    /// </summary>
    /// <param name="request">Write-off details (product, warehouse, quantity, reason)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created StockWriteOff record</returns>
    [HttpPost("writeoff")]
    [ProducesResponseType(typeof(Contracts.DTOs.StockWriteOffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WriteOff([FromBody] CreateStockWriteOffRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out int userId);

        var result = await _writeOffService.WriteOffExpiredStockAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
