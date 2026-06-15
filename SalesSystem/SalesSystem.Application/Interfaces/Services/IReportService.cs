using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

public interface IReportService
{
    /// <summary>
    /// Get sales report for a date range (Posted invoices only)
    /// </summary>
    Task<Result<IEnumerable<SalesReportDto>>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct);

    /// Get purchases report for a date range (Posted invoices only)
    Task<Result<IEnumerable<PurchaseReportDto>>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct);
    /// Get stock report for a specific warehouse or all warehouses
    Task<Result<IEnumerable<StockReportDto>>> GetStockReportAsync(int? warehouseId, CancellationToken ct);
    /// Get customer balances report
    Task<Result<IEnumerable<CustomerFinancialBalanceDto>>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct);
    /// Get supplier balances report
    Task<Result<IEnumerable<SupplierBalanceReportDto>>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct);
    /// Get product movements report for a specific product
    Task<Result<IEnumerable<ProductMovementReportDto>>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct);
    /// Get low stock products (below reorder level)
    Task<Result<IEnumerable<LowStockReportDto>>> GetLowStockReportAsync(int? warehouseId, CancellationToken ct);
    /// Get dashboard summary statistics
    Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken ct);

    /// <summary>
    /// Get expired products report with optional threshold (0 = already expired)
    /// </summary>
    Task<Result<IEnumerable<ExpiredProductDto>>> GetExpiredProductsReportAsync(int thresholdDays, CancellationToken ct);

    /// <summary>
    /// Get stock balance report — shows current stock, reorder level, average cost, and total value per product/warehouse
    /// </summary>
    Task<Result<List<StockBalanceReportDto>>> GetStockBalanceReportAsync(int? warehouseId, CancellationToken ct);

    /// <summary>
    /// Get warehouse movement report — shows inventory movements with quantities before/after
    /// </summary>
    Task<Result<List<WarehouseMovementReportDto>>> GetWarehouseMovementsAsync(int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct);

    // ─── Detailed Stock Ledger ──────────────────────────────────────────
    /// <summary>
    /// Get detailed stock ledger — full audit trail of inventory movements with running balances.
    /// </summary>
    Task<Result<List<DetailedStockLedgerDto>>> GetDetailedStockLedgerAsync(int? productId, int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct);

    // ─── Returns Report ─────────────────────────────────────────────────
    /// <summary>
    /// Get combined sales/purchase returns report.
    /// </summary>
    Task<Result<List<ReturnsReportDto>>> GetReturnsReportAsync(string? returnType, DateTime? from, DateTime? to, int? productId, CancellationToken ct);

    // ─── Aging Report ───────────────────────────────────────────────────
    /// <summary>
    /// Get customer/supplier aging report — balance aging buckets.
    /// </summary>
    Task<Result<List<AgingReportDto>>> GetAgingReportAsync(string partyType, int? partyId, CancellationToken ct);
}
