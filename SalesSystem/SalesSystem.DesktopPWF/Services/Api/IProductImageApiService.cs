using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IProductImageApiService
{
    Task<Result<List<ProductImageDto>>> GetByProductAsync(int productId, CancellationToken ct = default);
    Task<Result<ProductImageDto>> CreateAsync(CreateProductImageRequest request, CancellationToken ct = default);
    Task<Result> SetPrimaryAsync(int productId, int imageId, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
}
