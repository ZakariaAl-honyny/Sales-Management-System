using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IPurchaseReportApiService
{
    Task<Result<List<PurchasesBySupplierDto>>> GetPurchasesBySupplierAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<PurchasesByProductDto>>> GetPurchasesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<PurchaseTrendDto>>> GetPurchaseTrendsAsync(DateTime from, DateTime to, string groupBy = "day", CancellationToken ct = default);
}
