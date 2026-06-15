using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for purchase-related reports (Phase 31).
/// </summary>
public interface IPurchaseReportService
{
    /// <summary>
    /// Gets purchases grouped by supplier.
    /// </summary>
    Task<Result<List<PurchasesBySupplierDto>>> GetPurchasesBySupplierAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets purchases grouped by product with cost analysis.
    /// </summary>
    Task<Result<List<PurchasesByProductDto>>> GetPurchasesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets purchase trends grouped by period (monthly, quarterly).
    /// </summary>
    Task<Result<List<PurchaseTrendDto>>> GetPurchaseTrendsAsync(DateTime from, DateTime to, string groupBy, CancellationToken ct = default);
}
