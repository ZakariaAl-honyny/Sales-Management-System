using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for sales-related reports (Phase 31).
/// </summary>
public interface ISalesReportService
{
    /// <summary>
    /// Gets sales grouped by customer.
    /// </summary>
    Task<Result<List<SalesByCustomerDto>>> GetSalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets sales grouped by product with profit analysis.
    /// </summary>
    Task<Result<List<SalesByProductDto>>> GetSalesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets sales grouped by product category.
    /// </summary>
    Task<Result<List<SalesByCategoryDto>>> GetSalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets daily sales summary for a date range.
    /// </summary>
    Task<Result<List<DailySalesSummaryDto>>> GetDailySalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets sales trends grouped by period (monthly, quarterly).
    /// </summary>
    Task<Result<List<SalesTrendDto>>> GetSalesTrendsAsync(DateTime from, DateTime to, string groupBy, CancellationToken ct = default);

    /// <summary>
    /// Gets sales grouped by user (who created the invoice).
    /// </summary>
    Task<Result<List<SalesByUserDto>>> GetSalesByUserAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
