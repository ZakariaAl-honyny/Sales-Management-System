using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Repositories;

public interface IReportRepository
{
    // Sales Reports
    Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct);

    // Purchase Reports
    Task<IEnumerable<PurchaseReportDto>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct);
    // Stock Reports
    Task<IEnumerable<StockReportDto>> GetStockReportAsync(int? warehouseId, CancellationToken ct);
    Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int? warehouseId, CancellationToken ct);
    // Customer Balances
    Task<IEnumerable<CustomerFinancialBalanceDto>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct);
    // Supplier Balances
    Task<IEnumerable<SupplierBalanceReportDto>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct);
    // Product Movements
    Task<IEnumerable<ProductMovementReportDto>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct);
    // Stock Balance Report (v4.6.9+ Phase 26)
    Task<List<StockBalanceReportDto>> GetStockBalanceReportAsync(int? warehouseId, CancellationToken ct);
    // Warehouse Movement Report (v4.6.9+ Phase 26)
    Task<List<WarehouseMovementReportDto>> GetWarehouseMovementsAsync(int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct);
    // Dashboard Summary
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct);

    // ─── Detailed Stock Ledger ──────────────────────────────────────────
    /// <summary>
    /// Detailed stock ledger — full audit trail of inventory movements with running balances.
    /// </summary>
    Task<List<DetailedStockLedgerDto>> GetDetailedStockLedgerAsync(int? productId, int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct);

    // ─── Returns Report ─────────────────────────────────────────────────
    /// <summary>
    /// Combined sales/purchase returns report.
    /// </summary>
    Task<List<ReturnsReportDto>> GetReturnsReportAsync(string? returnType, DateTime? from, DateTime? to, int? productId, CancellationToken ct);

    // ─── Aging Report ───────────────────────────────────────────────────
    /// <summary>
    /// Customer/supplier aging report — balance aging buckets (Current, 1-30, 31-60, 61-90, 90+).
    /// </summary>
    Task<List<AgingReportDto>> GetAgingReportAsync(string partyType, int? partyId, CancellationToken ct);
}
