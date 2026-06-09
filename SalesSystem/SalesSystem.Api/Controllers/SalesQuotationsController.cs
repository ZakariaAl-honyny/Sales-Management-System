using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// عروض الأسعار (أوامر البيع) — إدارة عروض الأسعار من الإنشاء إلى التحويل لفاتورة.
/// </summary>
[ApiController]
[Route("api/v1/sales-quotations")]
[Authorize(Policy = "AllStaff")]
public class SalesQuotationsController : ControllerBase
{
    private readonly ISalesQuotationService _quotationService;
    private readonly ILogger<SalesQuotationsController> _logger;

    public SalesQuotationsController(
        ISalesQuotationService quotationService,
        ILogger<SalesQuotationsController> logger)
    {
        _quotationService = quotationService;
        _logger = logger;
    }

    /// <summary>الحصول على جميع عروض الأسعار مع الترشيح.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SalesQuotationDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] byte? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        _logger.LogInformation("طلب قائمة عروض الأسعار. العميل={CustomerId}, الحالة={Status}", customerId, status);
        var result = await _quotationService.GetAllAsync(customerId, status, search, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>الحصول على عرض سعر بواسطة المعرف.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        _logger.LogInformation("طلب عرض سعر المعرف {Id}", id);
        var result = await _quotationService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>إنشاء عرض سعر جديد (مسودة).</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSalesQuotationRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("إنشاء عرض سعر جديد بواسطة المستخدم {UserId}", userId);
        var result = await _quotationService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء عرض السعر بنجاح: المعرف {Id}", result.Value!.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        _logger.LogWarning("فشل إنشاء عرض السعر: {Error}", result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>تحديث عرض سعر موجود (المسودة فقط).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSalesQuotationRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("تحديث عرض السعر المعرف {Id} بواسطة المستخدم {UserId}", id, userId);
        var result = await _quotationService.UpdateAsync(id, request, userId, ct);

        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إلغاء عرض سعر.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        _logger.LogInformation("إلغاء عرض السعر المعرف {Id}", id);
        var result = await _quotationService.DeleteAsync(id, ct);

        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>تأكيد عرض السعر — ينقل الحالة من مسودة إلى مؤكد.</summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        _logger.LogInformation("تأكيد عرض السعر المعرف {Id}", id);
        var result = await _quotationService.ConfirmAsync(id, ct);

        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إنهاء صلاحية عرض السعر.</summary>
    [HttpPost("{id:int}/expire")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Expire(int id, CancellationToken ct)
    {
        _logger.LogInformation("إنهاء صلاحية عرض السعر المعرف {Id}", id);
        var result = await _quotationService.ExpireAsync(id, ct);

        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>تحويل عرض السعر المؤكد إلى فاتورة بيع.</summary>
    [HttpPost("{id:int}/convert-to-invoice")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConvertToInvoice(int id, [FromBody] ConvertQuotationToInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        _logger.LogInformation("تحويل عرض السعر {Id} إلى فاتورة بيع بواسطة المستخدم {UserId}", id, userId);
        var result = await _quotationService.ConvertToInvoiceAsync(id, request, userId, ct);

        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
