using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IProductPriceApiService
{
    Task<Result<List<ProductPriceDto>>> GetByProductUnitAsync(int productUnitId, CancellationToken ct = default);
    Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, CancellationToken ct = default);
    Task<Result<ProductPriceDto>> UpdateAsync(int id, UpdateProductPriceRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
}
