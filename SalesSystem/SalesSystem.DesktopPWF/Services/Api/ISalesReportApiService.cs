using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface ISalesReportApiService
{
    Task<Result<List<SalesByCustomerDto>>> GetSalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<SalesByProductDto>>> GetSalesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<SalesByCategoryDto>>> GetSalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<DailySalesSummaryDto>>> GetDailySalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<SalesTrendDto>>> GetSalesTrendsAsync(DateTime from, DateTime to, string groupBy = "day", CancellationToken ct = default);
    Task<Result<List<ProductProfitabilityDto>>> GetProductProfitabilityAsync(int? productId = null, int? categoryId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<List<ProfitByCustomerDto>>> GetProfitByCustomerAsync(int? customerId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}
