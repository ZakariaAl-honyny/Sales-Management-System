using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for generating financial reports (قائمة الدخل، التدفق النقدي، تقارير الضريبة، كشوف الحساب).
/// </summary>
[ApiController]
[Route("api/v1/financial-reports")]
[Authorize(Policy = "ManagerAndAbove")]
public class FinancialReportsController : ControllerBase
{
    private readonly IFinancialReportService _financialReportService;

    public FinancialReportsController(IFinancialReportService financialReportService)
    {
        _financialReportService = financialReportService;
    }

    /// <summary>
    /// Gets income statement (قائمة الدخل) for a date range.
    /// </summary>
    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _financialReportService.GetIncomeStatementAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets cash flow report (تقرير التدفق النقدي) for a date range.
    /// </summary>
    [HttpGet("cash-flow")]
    public async Task<IActionResult> GetCashFlowReport([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? cashBoxId, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _financialReportService.GetCashFlowReportAsync(from, to, cashBoxId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets VAT report (تقرير الضريبة) for a date range.
    /// </summary>
    [HttpGet("vat-report")]
    public async Task<IActionResult> GetVatReport([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _financialReportService.GetVatReportAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets customer account statement (كشف حساب عميل) for a date range.
    /// </summary>
    [HttpGet("account-statement/customer/{customerId:int}")]
    public async Task<IActionResult> GetAccountStatement(int customerId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        if (customerId <= 0)
            return BadRequest(new { error = "معرف العميل غير صالح" });

        var result = await _financialReportService.GetAccountStatementAsync(customerId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets supplier account statement (كشف حساب مورد) for a date range.
    /// </summary>
    [HttpGet("account-statement/supplier/{supplierId:int}")]
    public async Task<IActionResult> GetSupplierStatement(int supplierId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        if (supplierId <= 0)
            return BadRequest(new { error = "معرف المورد غير صالح" });

        var result = await _financialReportService.GetSupplierStatementAsync(supplierId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
