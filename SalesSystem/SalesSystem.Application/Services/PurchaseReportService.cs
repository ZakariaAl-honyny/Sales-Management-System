using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class PurchaseReportService : IPurchaseReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PurchaseReportService> _logger;

    public PurchaseReportService(IUnitOfWork uow, ILogger<PurchaseReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<PurchasesBySupplierDto>>> GetPurchasesBySupplierAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<PurchasesBySupplierDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting purchases by supplier from {From} to {To}", from, to);

            var invoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted && pi.InvoiceDate >= DateOnly.FromDateTime(from) && pi.InvoiceDate <= DateOnly.FromDateTime(to),
                q => q.Include(pi => pi.Supplier).ThenInclude(s => s!.Party),
                ct);

            var grouped = invoices
                .GroupBy(pi => new { pi.SupplierId, SupplierName = pi.Supplier?.Party?.Name ?? "مورد" })
                .Select(g => new PurchasesBySupplierDto(
                    g.Key.SupplierId,
                    g.Key.SupplierName,
                    g.Count(),
                    g.Sum(pi => pi.NetTotal),
                    g.Sum(pi => pi.PaidAmount),
                    g.Sum(pi => pi.RemainingAmount)
                ))
                .OrderByDescending(dto => dto.TotalAmount)
                .ToList();

            return Result<List<PurchasesBySupplierDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purchases by supplier report");
            return Result<List<PurchasesBySupplierDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المشتريات حسب المورد");
        }
    }

    public async Task<Result<List<PurchasesByProductDto>>> GetPurchasesByProductAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<PurchasesByProductDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting purchases by product from {From} to {To}", from, to);

            var invoiceItems = await _uow.PurchaseInvoiceLines.ToListAsync(
                item => item.PurchaseInvoice!.Status == InvoiceStatus.Posted
                     && item.PurchaseInvoice!.InvoiceDate >= DateOnly.FromDateTime(from)
                     && item.PurchaseInvoice!.InvoiceDate <= DateOnly.FromDateTime(to),
                q => q.Include(item => item.PurchaseInvoice).Include(item => item.Product),
                ct);

            var grouped = invoiceItems
                .GroupBy(item => new { item.ProductId, ProductName = item.Product != null ? item.Product.Name : "غير معروف" })
                .Select(g => new PurchasesByProductDto(
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Sum(item => item.Quantity),
                    g.Sum(item => item.LineTotal)
                ))
                .OrderByDescending(dto => dto.TotalCost)
                .ToList();

            return Result<List<PurchasesByProductDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purchases by product report");
            return Result<List<PurchasesByProductDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المشتريات حسب المنتج");
        }
    }

    public async Task<Result<List<PurchaseTrendDto>>> GetPurchaseTrendsAsync(DateTime from, DateTime to, string groupBy, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<PurchaseTrendDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting purchase trends from {From} to {To} grouped by {GroupBy}", from, to, groupBy);

            var invoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted && pi.InvoiceDate >= DateOnly.FromDateTime(from) && pi.InvoiceDate <= DateOnly.FromDateTime(to),
                ct: ct);

            Func<DateOnly, string> keySelector = groupBy?.ToLower() switch
            {
                "monthly" or "month" => dt => dt.ToString("yyyy-MM"),
                "quarterly" or "quarter" => dt => $"{dt.Year}-Q{(dt.Month - 1) / 3 + 1}",
                "yearly" or "year" => dt => dt.ToString("yyyy"),
                _ => dt => dt.ToString("yyyy-MM")
            };

            var grouped = invoices
                .GroupBy(pi => keySelector(pi.InvoiceDate))
                .Select(g => new PurchaseTrendDto(
                    g.Key,
                    g.Sum(pi => pi.NetTotal),
                    g.Sum(pi => pi.NetTotal) // Using NetTotal as TotalCost for purchases
                ))
                .OrderBy(dto => dto.Period)
                .ToList();

            return Result<List<PurchaseTrendDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purchase trends");
            return Result<List<PurchaseTrendDto>>.Failure("حدث خطأ أثناء إنشاء اتجاهات المشتريات");
        }
    }
}
