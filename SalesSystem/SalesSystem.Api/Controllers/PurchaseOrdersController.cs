using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// أوامر الشراء — إدارة أوامر الشراء من الإنشاء إلى الإلغاء.
/// </summary>
[ApiController]
[Route("api/v1/purchase-orders")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    /// <summary>الحصول على جميع أوامر الشراء مع الترشيح.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PurchaseOrderDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? supplierId,
        [FromQuery] byte? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        _logger.LogInformation("طلب قائمة أوامر الشراء. المورد={SupplierId}, الحالة={Status}", supplierId, status);
        var result = await _purchaseOrderService.GetAllAsync(supplierId, status, search, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>الحصول على أمر شراء بواسطة المعرف.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        _logger.LogInformation("طلب أمر شراء المعرف {Id}", id);
        var result = await _purchaseOrderService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>الحصول على أوامر الشراء المعلقة (المعتمدة والمستلمة جزئياً).</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PurchaseOrderDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetPendingOrders(CancellationToken ct)
    {
        var result = await _purchaseOrderService.GetPendingOrdersAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>إنشاء أمر شراء جديد (مسودة).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("إنشاء أمر شراء جديد بواسطة المستخدم {UserId}", userId);
        var result = await _purchaseOrderService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء أمر الشراء بنجاح: المعرف {Id}", result.Value!.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        _logger.LogWarning("فشل إنشاء أمر الشراء: {Error}", result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>تحديث أمر شراء موجود (المسودة فقط).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("تحديث أمر الشراء المعرف {Id} بواسطة المستخدم {UserId}", id, userId);
        var result = await _purchaseOrderService.UpdateAsync(id, request, userId, ct);

        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إلغاء أمر شراء.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("إلغاء أمر الشراء المعرف {Id} بواسطة المستخدم {UserId}", id, userId);
        var result = await _purchaseOrderService.CancelAsync(id, userId, ct);

        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
