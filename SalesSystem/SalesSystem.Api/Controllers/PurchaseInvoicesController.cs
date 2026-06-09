using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// فواتير المشتريات — إدارة فواتير الشراء مع دعم العملات المتعددة والمرفقات والمصاريف الإضافية.
/// </summary>
[ApiController]
[Route("api/v1/purchase-invoices")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IAdditionalFeeService _additionalFeeService;
    private readonly ILogger<PurchaseInvoicesController> _logger;

    public PurchaseInvoicesController(
        IPurchaseService purchaseService,
        IAdditionalFeeService additionalFeeService,
        ILogger<PurchaseInvoicesController> logger)
    {
        _purchaseService = purchaseService;
        _additionalFeeService = additionalFeeService;
        _logger = logger;
    }

    #region Invoice CRUD

    /// <summary>الحصول على قائمة فواتير الشراء مع الترشيح والترقيم.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseInvoiceDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? supplierId,
        [FromQuery] int? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _purchaseService.GetAllAsync(supplierId, status, search, from, to, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>الحصول على فاتورة شراء بواسطة المعرف.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _purchaseService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>إنشاء فاتورة شراء جديدة (مسودة).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء فاتورة شراء كمسودة: المعرف {Id} بواسطة المستخدم {UserId}", result.Value!.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }
        return BadRequest(new { error = result.Error });
    }

    /// <summary>تحديث فاتورة شراء موجودة (المسودة فقط).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.UpdateAsync(id, request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>ترحيل فاتورة شراء (إضافة المخزون وتحديث رصيد المورد).</summary>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.PostAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إلغاء فاتورة شراء (عكس المخزون والأرصدة).</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.CancelAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    #endregion

    #region Attachments

    /// <summary>رفع مرفق لفاتورة الشراء.</summary>
    [HttpPost("{id:int}/upload-attachment")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadAttachment(int id, [FromBody] UploadAttachmentRequest request, CancellationToken ct)
    {
        var result = await _purchaseService.UploadAttachmentAsync(id, request.Base64Content, request.FileName, ct);
        if (result.IsSuccess) return Ok(new { path = result.Value });
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>حذف مرفق فاتورة الشراء.</summary>
    [HttpDelete("{id:int}/attachment")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAttachment(int id, CancellationToken ct)
    {
        var result = await _purchaseService.DeleteAttachmentAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    #endregion

    #region Additional Fees

    /// <summary>الحصول على المصاريف الإضافية لفاتورة الشراء.</summary>
    [HttpGet("{id:int}/fees")]
    [ProducesResponseType(typeof(List<AdditionalFeeDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFees(int id, CancellationToken ct)
    {
        var result = await _additionalFeeService.GetFeesByInvoiceAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>إضافة مصروف إضافي لفاتورة الشراء.</summary>
    [HttpPost("{id:int}/fees")]
    [ProducesResponseType(typeof(AdditionalFeeDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateFee(int id, [FromBody] CreateAdditionalFeeRequest request, CancellationToken ct)
    {
        var result = await _additionalFeeService.CreateFeeAsync(request, id, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إضافة مصروف إضافي {FeeName} للفاتورة {InvoiceId}", request.FeeName, id);
            return CreatedAtAction(nameof(GetFees), new { id }, result.Value);
        }
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إزالة مصروف إضافي من فاتورة الشراء.</summary>
    [HttpDelete("{id:int}/fees/{feeId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveFee(int id, int feeId, CancellationToken ct)
    {
        var result = await _additionalFeeService.RemoveFeeAsync(feeId, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إزالة المصروف الإضافي {FeeId} من الفاتورة {InvoiceId}", feeId, id);
            return Ok();
        }
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    #endregion
}

/// <summary>
/// طلب رفع مرفق.
/// </summary>
public record UploadAttachmentRequest(string Base64Content, string? FileName);
