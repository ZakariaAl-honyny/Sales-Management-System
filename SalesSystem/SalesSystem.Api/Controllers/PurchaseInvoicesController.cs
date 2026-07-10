using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// فواتير المشتريات — إدارة فواتير الشراء مع دعم العملات المتعددة والمرفقات.
/// </summary>
[ApiController]
[Route("api/v1/purchase-invoices")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly ILogger<PurchaseInvoicesController> _logger;

    public PurchaseInvoicesController(
        IPurchaseService purchaseService,
        ILogger<PurchaseInvoicesController> logger)
    {
        _purchaseService = purchaseService;
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
        var userId = GetUserId();
        var result = await _purchaseService.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء فاتورة شراء كمسودة: المعرف {Id} بواسطة المستخدم {UserId}", result.Value!.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إنشاء وترحيل فاتورة شراء في خطوة واحدة.</summary>
    [HttpPost("create-and-post")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateAndPost([FromBody] CreatePurchaseInvoiceRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _purchaseService.CreateAndPostAsync(request, userId, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء وترحيل فاتورة الشراء: المعرف {Id} بواسطة المستخدم {UserId}", result.Value!.Id, userId);
            return Ok(result.Value);
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
        var userId = GetUserId();
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
        var userId = GetUserId();
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
        var userId = GetUserId();
        var result = await _purchaseService.CancelAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    #endregion

    /// <summary>رفع ملف مرفق لفاتورة الشراء.</summary>
    [HttpPost("{id:int}/upload-attachment")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "الملف مطلوب" });

        var uploadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SalesSystem", "PurchaseAttachments", id.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Update invoice with attachment path
        var result = await _purchaseService.SetAttachmentAsync(id, filePath, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("تم رفع المرفق لفاتورة الشراء {Id}: {Path}", id, filePath);
            return Ok(new { path = filePath });
        }
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Extracts user ID from JWT claims.
    /// </summary>
    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User not authenticated — JWT claim missing.");
    }
}
