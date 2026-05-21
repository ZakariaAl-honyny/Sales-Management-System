using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IReportApiService
{
    Task<Result<IReadOnlyList<SalesReportDto>>> GetSalesAsync(DateTime? from = null, DateTime? to = null, int? customerId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PurchaseReportDto>>> GetPurchasesAsync(DateTime? from = null, DateTime? to = null, int? supplierId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StockReportDto>>> GetStockReportAsync(int? warehouseId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CustomerBalanceReportDto>>> GetCustomerBalancesAsync(int? customerId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SupplierBalanceReportDto>>> GetSupplierBalancesAsync(int? supplierId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductMovementReportDto>>> GetMovementsAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LowStockReportDto>>> GetLowStockAsync(CancellationToken ct = default);
}
