using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class SalesReportService : ISalesReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SalesReportService> _logger;

    public SalesReportService(IUnitOfWork uow, ILogger<SalesReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<SalesByCustomerDto>>> GetSalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<SalesByCustomerDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting sales by customer from {From} to {To}", from, to);

            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted && si.InvoiceDate >= from && si.InvoiceDate <= to,
                q => q.Include(si => si.Customer),
                ct);

            var grouped = invoices
                .GroupBy(si => new { si.CustomerId, CustomerName = si.Customer != null ? si.Customer.Name : "عميل نقدي" })
                .Select(g => new SalesByCustomerDto(
                    g.Key.CustomerId ?? 0,
                    g.Key.CustomerName,
                    g.Count(),
                    g.Sum(si => si.TotalAmount),
                    g.Sum(si => si.PaidAmount),
                    g.Sum(si => si.DueAmount)
                ))
                .OrderByDescending(dto => dto.TotalAmount)
                .ToList();

            return Result<List<SalesByCustomerDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales by customer report");
            return Result<List<SalesByCustomerDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المبيعات حسب العميل");
        }
    }

    public async Task<Result<List<SalesByProductDto>>> GetSalesByProductAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<SalesByProductDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting sales by product from {From} to {To}", from, to);

            var invoiceItems = await _uow.SalesInvoiceItems.ToListAsync(
                item => item.SalesInvoice.Status == InvoiceStatus.Posted
                     && item.SalesInvoice.InvoiceDate >= from
                     && item.SalesInvoice.InvoiceDate <= to,
                q => q.Include(item => item.SalesInvoice).Include(item => item.Product),
                ct);

            var grouped = invoiceItems
                .GroupBy(item => new { item.ProductId, ProductName = item.Product != null ? item.Product.Name : "غير معروف" })
                .AsEnumerable()
                .Select(g =>
                {
                    var totalAmount = g.Sum(item => item.LineTotal);
                    var totalCost = g.Sum(item => item.CostInBaseCurrency ?? 0);
                    var totalProfit = g.Sum(item => item.Profit != 0 ? item.Profit : item.LineTotal - (item.CostInBaseCurrency ?? 0));
                    var quantity = g.Sum(item => item.Quantity);
                    var profitMargin = totalAmount > 0 ? Math.Round(totalProfit / totalAmount * 100, 2) : 0;

                    return new SalesByProductDto(
                        g.Key.ProductId,
                        g.Key.ProductName,
                        quantity,
                        totalAmount,
                        totalCost,
                        totalProfit,
                        profitMargin
                    );
                })
                .OrderByDescending(dto => dto.TotalAmount)
                .ToList();

            return Result<List<SalesByProductDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales by product report");
            return Result<List<SalesByProductDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المبيعات حسب المنتج");
        }
    }

    public async Task<Result<List<SalesByCategoryDto>>> GetSalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<SalesByCategoryDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting sales by category from {From} to {To}", from, to);

            var invoiceItems = await _uow.SalesInvoiceItems.ToListAsync(
                item => item.SalesInvoice.Status == InvoiceStatus.Posted
                     && item.SalesInvoice.InvoiceDate >= from
                     && item.SalesInvoice.InvoiceDate <= to,
                q => q.Include(item => item.SalesInvoice)
                      .Include(item => item.Product)
                      .ThenInclude(p => p!.Category),
                ct);

            var grouped = invoiceItems
                .Where(item => item.Product?.Category != null)
                .GroupBy(item => new { item.Product!.Category!.Id, item.Product.Category.Name })
                .Select(g => new SalesByCategoryDto(
                    g.Key.Id,
                    g.Key.Name,
                    g.Select(item => item.SalesInvoiceId).Distinct().Count(),
                    g.Sum(item => item.LineTotal)
                ))
                .OrderByDescending(dto => dto.TotalAmount)
                .ToList();

            // Include uncategorized items
            var uncategorizedItems = invoiceItems.Where(item => item.Product?.Category == null).ToList();
            if (uncategorizedItems.Count > 0)
            {
                grouped.Add(new SalesByCategoryDto(
                    0, "غير مصنف",
                    uncategorizedItems.Select(item => item.SalesInvoiceId).Distinct().Count(),
                    uncategorizedItems.Sum(item => item.LineTotal)
                ));
            }

            return Result<List<SalesByCategoryDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales by category report");
            return Result<List<SalesByCategoryDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المبيعات حسب التصنيف");
        }
    }

    public async Task<Result<List<DailySalesSummaryDto>>> GetDailySalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<DailySalesSummaryDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting daily sales summary from {From} to {To}", from, to);

            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted && si.InvoiceDate >= from && si.InvoiceDate <= to,
                ct: ct);

            var grouped = invoices
                .GroupBy(si => si.InvoiceDate.Date)
                .Select(g => new DailySalesSummaryDto(
                    g.Key,
                    g.Count(),
                    g.Sum(si => si.TotalAmount),
                    g.Sum(si => si.DiscountAmount),
                    g.Sum(si => si.TotalAmount - si.DiscountAmount)
                ))
                .OrderBy(dto => dto.Date)
                .ToList();

            return Result<List<DailySalesSummaryDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily sales summary");
            return Result<List<DailySalesSummaryDto>>.Failure("حدث خطأ أثناء إنشاء ملخص المبيعات اليومي");
        }
    }

    public async Task<Result<List<SalesTrendDto>>> GetSalesTrendsAsync(DateTime from, DateTime to, string groupBy, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<SalesTrendDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting sales trends from {From} to {To} grouped by {GroupBy}", from, to, groupBy);

            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted && si.InvoiceDate >= from && si.InvoiceDate <= to,
                ct: ct);

            // Get items for cost calculation
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            var items = await _uow.SalesInvoiceItems.ToListAsync(
                item => invoiceIds.Contains(item.SalesInvoiceId),
                ct: ct);
            var itemCostsByInvoice = items
                .GroupBy(item => item.SalesInvoiceId)
                .ToDictionary(g => g.Key, g => g.Sum(item => item.CostInBaseCurrency ?? 0));

            Func<DateTime, string> keySelector = groupBy?.ToLower() switch
            {
                "monthly" or "month" => dt => dt.ToString("yyyy-MM"),
                "quarterly" or "quarter" => dt => $"{dt.Year}-Q{(dt.Month - 1) / 3 + 1}",
                "yearly" or "year" => dt => dt.ToString("yyyy"),
                _ => dt => dt.ToString("yyyy-MM")
            };

            var grouped = invoices
                .GroupBy(si => keySelector(si.InvoiceDate))
                .Select(g =>
                {
                    var totalSales = g.Sum(si => si.TotalAmount);
                    var totalCost = g.Sum(si => itemCostsByInvoice.GetValueOrDefault(si.Id, 0));
                    var totalProfit = totalSales - totalCost;
                    var profitMargin = totalSales > 0 ? Math.Round(totalProfit / totalSales * 100, 2) : 0;

                    return new SalesTrendDto(g.Key, totalSales, totalCost, totalProfit, profitMargin);
                })
                .OrderBy(dto => dto.Period)
                .ToList();

            return Result<List<SalesTrendDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales trends");
            return Result<List<SalesTrendDto>>.Failure("حدث خطأ أثناء إنشاء اتجاهات المبيعات");
        }
    }

    public async Task<Result<List<SalesByUserDto>>> GetSalesByUserAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<SalesByUserDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting sales by user from {From} to {To}", from, to);

            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted && si.InvoiceDate >= from && si.InvoiceDate <= to,
                ct: ct);

            // Get users for name lookup
            var userIds = invoices.Where(si => si.CreatedByUserId.HasValue).Select(si => si.CreatedByUserId!.Value).Distinct().ToList();
            var users = userIds.Count > 0
                ? await _uow.Users.ToListAsync(u => userIds.Contains(u.Id), ct: ct)
                : new List<Domain.Entities.User>();
            var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

            var grouped = invoices
                .GroupBy(si => si.CreatedByUserId)
                .Select(g =>
                {
                    var userId = g.Key ?? 0;
                    return new SalesByUserDto(
                        userId,
                        userDict.GetValueOrDefault(userId, $"مستخدم #{userId}"),
                        g.Count(),
                        g.Sum(si => si.TotalAmount)
                    );
                })
                .OrderByDescending(dto => dto.TotalAmount)
                .ToList();

            return Result<List<SalesByUserDto>>.Success(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales by user report");
            return Result<List<SalesByUserDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المبيعات حسب المستخدم");
        }
    }
}
