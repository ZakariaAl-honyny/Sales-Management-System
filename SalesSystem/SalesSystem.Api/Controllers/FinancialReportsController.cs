using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for generating financial reports (قائمة الدخل، الميزانية، ميزان المراجعة، التدفق النقدي، تقارير الضريبة، كشوف الحساب).
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
    /// Gets hierarchical income statement (قائمة الدخل الهرمية) — RULE-422.
    /// Revenue - COGS = GrossProfit - OperatingExpenses = NetIncome with subtotals.
    /// </summary>
    [HttpGet("income-statement-hierarchy")]
    public async Task<IActionResult> GetIncomeStatementHierarchy([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _financialReportService.GetIncomeStatementHierarchyAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets balance sheet (الميزانية العمومية) as of a date — RULE-423.
    /// Assets = Liabilities + Equity with section subtotals.
    /// </summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime asOfDate, CancellationToken ct)
    {
        var result = await _financialReportService.GetBalanceSheetAsync(asOfDate, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets trial balance (ميزان المراجعة) as of a date.
    /// Shows opening balances, transactions, and closing balances for all accounts.
    /// </summary>
    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime asOfDate, CancellationToken ct)
    {
        var result = await _financialReportService.GetTrialBalanceAsync(asOfDate, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets general ledger (دفتر الأستاذ العام) for a specific account.
    /// </summary>
    [HttpGet("general-ledger/{accountId:int:min(1)}")]
    public async Task<IActionResult> GetGeneralLedger(int accountId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _financialReportService.GetGeneralLedgerAsync(accountId, from, to, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "NotFound" ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
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
