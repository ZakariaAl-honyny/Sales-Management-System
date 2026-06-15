using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service for cash box-related reports — summary, receipt vouchers, and payment vouchers.
/// Uses IUnitOfWork for all data access (no direct DbContext injection).
/// </summary>
public class CashBoxReportService : ICashBoxReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CashBoxReportService> _logger;

    public CashBoxReportService(IUnitOfWork uow, ILogger<CashBoxReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ─── Cash Box Summary ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating cash box summary as of {AsOfDate}",
                asOfDate ?? DateTime.Today);

            var effectiveDate = asOfDate ?? DateTime.Today;

            // Get all active cash boxes
            var cashBoxes = await _uow.CashBoxes.ToListAsync(ct: ct);

            if (cashBoxes.Count == 0)
            {
                _logger.LogWarning("No cash boxes found for summary report");
                return Result<List<CashBoxSummaryDto>>.Success(new List<CashBoxSummaryDto>());
            }

            var cashBoxIds = cashBoxes.Select(cb => cb.Id).ToList();

            // Get posted receipt vouchers (income) up to the effective date
            var receiptVouchers = await _uow.ReceiptVouchers.ToListAsync(
                rv => rv.Status == (byte)InvoiceStatus.Posted
                   && rv.VoucherDate <= effectiveDate
                   && cashBoxIds.Contains(rv.CashBoxId),
                ct: ct);

            // Get posted payment vouchers (expenses) up to the effective date
            var paymentVouchers = await _uow.PaymentVouchers.ToListAsync(
                pv => pv.Status == (byte)InvoiceStatus.Posted
                   && pv.VoucherDate <= effectiveDate
                   && cashBoxIds.Contains(pv.CashBoxId),
                ct: ct);

            // Group receipts and payments by cash box
            var incomeByBox = receiptVouchers
                .GroupBy(rv => rv.CashBoxId)
                .ToDictionary(g => g.Key, g => g.Sum(rv => rv.TotalAmount));

            var expenseByBox = paymentVouchers
                .GroupBy(pv => pv.CashBoxId)
                .ToDictionary(g => g.Key, g => g.Sum(pv => pv.TotalAmount));

            var results = cashBoxes.Select(cb =>
            {
                var totalIncome = incomeByBox.GetValueOrDefault(cb.Id, 0m);
                var totalExpense = expenseByBox.GetValueOrDefault(cb.Id, 0m);
                return new CashBoxSummaryDto(
                    CashBoxId: cb.Id,
                    CashBoxName: cb.Name,
                    TotalIncome: totalIncome,
                    TotalExpense: totalExpense,
                    NetBalance: totalIncome - totalExpense);
            }).ToList();

            _logger.LogInformation("Cash box summary generated: {Count} boxes", results.Count);
            return Result<List<CashBoxSummaryDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash box summary");
            return Result<List<CashBoxSummaryDto>>.Failure(
                "حدث خطأ أثناء إنشاء ملخص الصناديق النقدية");
        }
    }

    // ─── Receipt Voucher Report ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<List<ReceiptVoucherReportDto>>> GetReceiptVoucherReportAsync(
        DateTime from,
        DateTime to,
        int? cashBoxId = null,
        CancellationToken ct = default)
    {
        try
        {
            if (from > to)
            {
                _logger.LogWarning("Receipt voucher report failed: start date {From} after end date {To}", from, to);
                return Result<List<ReceiptVoucherReportDto>>.Failure(
                    "تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            }

            _logger.LogInformation(
                "Generating receipt voucher report from {From} to {To} for cash box {CashBoxId}",
                from, to, cashBoxId);

            var vouchers = await _uow.ReceiptVouchers.ToListAsync(
                rv => rv.VoucherDate >= from
                   && rv.VoucherDate <= to
                   && (!cashBoxId.HasValue || rv.CashBoxId == cashBoxId.Value),
                q => q.OrderByDescending(rv => rv.VoucherDate),
                ct: ct,
                includePaths: new[] { "CashBox", "Account" });

            var dtos = vouchers.Select(MapToReceiptDto).ToList();

            _logger.LogInformation("Receipt voucher report generated: {Count} vouchers", dtos.Count);
            return Result<List<ReceiptVoucherReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt voucher report");
            return Result<List<ReceiptVoucherReportDto>>.Failure(
                "حدث خطأ أثناء إنشاء تقرير سندات القبض");
        }
    }

    // ─── Payment Voucher Report ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<List<PaymentVoucherReportDto>>> GetPaymentVoucherReportAsync(
        DateTime from,
        DateTime to,
        int? cashBoxId = null,
        CancellationToken ct = default)
    {
        try
        {
            if (from > to)
            {
                _logger.LogWarning("Payment voucher report failed: start date {From} after end date {To}", from, to);
                return Result<List<PaymentVoucherReportDto>>.Failure(
                    "تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            }

            _logger.LogInformation(
                "Generating payment voucher report from {From} to {To} for cash box {CashBoxId}",
                from, to, cashBoxId);

            var vouchers = await _uow.PaymentVouchers.ToListAsync(
                pv => pv.VoucherDate >= from
                   && pv.VoucherDate <= to
                   && (!cashBoxId.HasValue || pv.CashBoxId == cashBoxId.Value),
                q => q.OrderByDescending(pv => pv.VoucherDate),
                ct: ct,
                includePaths: new[] { "CashBox", "Account" });

            var dtos = vouchers.Select(MapToPaymentDto).ToList();

            _logger.LogInformation("Payment voucher report generated: {Count} vouchers", dtos.Count);
            return Result<List<PaymentVoucherReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment voucher report");
            return Result<List<PaymentVoucherReportDto>>.Failure(
                "حدث خطأ أثناء إنشاء تقرير سندات الصرف");
        }
    }

    // ─── Private Mappers ───────────────────────────────────────────────────────

    private static ReceiptVoucherReportDto MapToReceiptDto(ReceiptVoucher voucher)
    {
        return new ReceiptVoucherReportDto(
            Id: voucher.Id,
            VoucherNo: voucher.VoucherNo,
            VoucherDate: voucher.VoucherDate,
            CashBoxName: voucher.CashBox?.Name ?? string.Empty,
            AccountName: voucher.Account?.NameAr ?? string.Empty,
            TotalAmount: voucher.TotalAmount,
            Notes: voucher.Notes,
            StatusDisplay: GetStatusDisplay(voucher.Status));
    }

    private static PaymentVoucherReportDto MapToPaymentDto(PaymentVoucher voucher)
    {
        return new PaymentVoucherReportDto(
            Id: voucher.Id,
            VoucherNo: voucher.VoucherNo,
            VoucherDate: voucher.VoucherDate,
            CashBoxName: voucher.CashBox?.Name ?? string.Empty,
            AccountName: voucher.Account?.NameAr ?? string.Empty,
            TotalAmount: voucher.TotalAmount,
            Notes: voucher.Notes,
            StatusDisplay: GetStatusDisplay(voucher.Status));
    }

    /// <summary>
    /// Converts the byte Status to an Arabic display name.
    /// Status values: 1=Draft, 2=Posted, 3=Cancelled.
    /// </summary>
    private static string GetStatusDisplay(byte status) => status switch
    {
        1 => "مسودة",
        2 => "مرحّل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}
