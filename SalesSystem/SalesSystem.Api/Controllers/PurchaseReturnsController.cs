using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// مرتجعات المشتريات — إدارة مرتجعات المشتريات من الإنشاء إلى الإلغاء.
/// القراءة متاحة لجميع الموظفين، والكتابة للمديرين فما فوق.
/// </summary>
[ApiController]
[Route("api/v1/purchase-returns")]
[Authorize]
public class PurchaseReturnsController : ControllerBase
{
    private readonly IPurchaseReturnService _purchaseReturnService;
    private readonly ILogger<PurchaseReturnsController> _logger;

    public PurchaseReturnsController(
        IPurchaseReturnService purchaseReturnService,
        ILogger<PurchaseReturnsController> logger)
    {
        _purchaseReturnService = purchaseReturnService;
        _logger = logger;
    }

    /// <summary>الحصول على جميع مرتجعات المشتريات مع الترشيح والترقيد.</summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<PurchaseReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("طلب قائمة مرتجعات المشتريات. المعرف={SupplierId}, الصفحة={Page}, الحجم={PageSize}, نشط={IncludeInactive}",
            supplierId, page, pageSize, includeInactive);

        var result = await _purchaseReturnService.GetAllAsync(supplierId, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>الحصول على مرتجع شراء بواسطة المعرف.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        _logger.LogInformation("طلب مرتجع شراء المعرف {Id}", id);

        var result = await _purchaseReturnService.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>إنشاء مرتجع شراء جديد (مسودة).</summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseReturnRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("فشل إنشاء مرتجع الشراء: رمز المستخدم غير صالح");
            return Unauthorized();
        }

        _logger.LogInformation("إنشاء مرتجع شراء جديد بواسطة المستخدم {UserId}. فاتورة الشراء={PurchaseInvoiceId}, عدد البنود={ItemCount}",
            userId, request.PurchaseInvoiceId, request.Items?.Count ?? 0);

        var result = await _purchaseReturnService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إنشاء مرتجع الشراء بنجاح: المعرف {Id}", result.Value!.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        _logger.LogWarning("فشل إنشاء مرتجع الشراء للمستخدم {UserId}: {Error}", userId, result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>ترحيل مرتجع شراء من حالة المسودة إلى حالة الترحيل (تأثير على المخزون ورصيد المورد).</summary>
    [HttpPost("{id:int}/post")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("فشل ترحيل مرتجع الشراء {Id}: رمز المستخدم غير صالح", id);
            return Unauthorized();
        }

        _logger.LogInformation("ترحيل مرتجع الشراء المعرف {Id} بواسطة المستخدم {UserId}", id, userId);

        var result = await _purchaseReturnService.PostAsync(id, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("تم ترحيل مرتجع الشراء {Id} بنجاح", id);
            return Ok(result.Value);
        }

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });

        _logger.LogWarning("فشل ترحيل مرتجع الشراء {Id}: {Error}", id, result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>إلغاء مرتجع شراء (عكس تأثير المخزون ورصيد المورد إذا كان مرحلاً).</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("فشل إلغاء مرتجع الشراء {Id}: رمز المستخدم غير صالح", id);
            return Unauthorized();
        }

        _logger.LogInformation("إلغاء مرتجع الشراء المعرف {Id} بواسطة المستخدم {UserId}", id, userId);

        var result = await _purchaseReturnService.CancelAsync(id, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("تم إلغاء مرتجع الشراء {Id} بنجاح", id);
            return Ok(result.Value);
        }

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });

        _logger.LogWarning("فشل إلغاء مرتجع الشراء {Id}: {Error}", id, result.Error);
        return BadRequest(new { error = result.Error });
    }
}
