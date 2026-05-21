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
    Task<IEnumerable<CustomerBalanceReportDto>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct);
    // Supplier Balances
    Task<IEnumerable<SupplierBalanceReportDto>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct);
    // Product Movements
    Task<IEnumerable<ProductMovementReportDto>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct);
    // Dashboard Summary
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct);
}
