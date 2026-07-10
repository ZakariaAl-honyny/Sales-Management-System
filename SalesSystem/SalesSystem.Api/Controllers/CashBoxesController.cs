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

    // ═══════════════════════════════════════════
    // Cash Box CRUD
    // ═══════════════════════════════════════════

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

    [HttpDelete("permanent/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _service.PermanentDeleteAsync(id, ct);
        if (result.IsSuccess) return Ok(new { message = "تم حذف الصندوق نهائياً" });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);
        if (result.IsSuccess) return Ok(new { message = "تم استعادة الصندوق بنجاح" });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    // ═══════════════════════════════════════════
    // Receipt Vouchers (سندات قبض)
    // ═══════════════════════════════════════════

    [HttpPost("{id:int}/receipt-vouchers")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> CreateReceiptVoucher(int id, [FromBody] CreateReceiptVoucherRequest request, CancellationToken ct)
    {
        if (id != request.CashBoxId)
            return BadRequest(new { error = "معرف الصندوق غير متطابق" });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CreateReceiptVoucherAsync(request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/receipt-vouchers/{voucherId:int}/post")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PostReceiptVoucher(int id, int voucherId, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.PostReceiptVoucherAsync(voucherId, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/receipt-vouchers/{voucherId:int}/cancel")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> CancelReceiptVoucher(int id, int voucherId, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CancelReceiptVoucherAsync(voucherId, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}/receipt-vouchers")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetReceiptVouchers(int id, CancellationToken ct)
    {
        var result = await _service.GetReceiptVouchersAsync(id, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    // ═══════════════════════════════════════════
    // Payment Vouchers (سندات صرف)
    // ═══════════════════════════════════════════

    [HttpPost("{id:int}/payment-vouchers")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> CreatePaymentVoucher(int id, [FromBody] CreatePaymentVoucherRequest request, CancellationToken ct)
    {
        if (id != request.CashBoxId)
            return BadRequest(new { error = "معرف الصندوق غير متطابق" });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CreatePaymentVoucherAsync(request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/payment-vouchers/{voucherId:int}/post")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> PostPaymentVoucher(int id, int voucherId, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.PostPaymentVoucherAsync(voucherId, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/payment-vouchers/{voucherId:int}/cancel")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> CancelPaymentVoucher(int id, int voucherId, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CancelPaymentVoucherAsync(voucherId, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}/payment-vouchers")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetPaymentVouchers(int id, CancellationToken ct)
    {
        var result = await _service.GetPaymentVouchersAsync(id, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    // ═══════════════════════════════════════════
    // Transfers
    // ═══════════════════════════════════════════

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
}
