using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Services;

public interface IProductUnitService
{
    Task<Result<List<ProductUnitDto>>> GetByProductIdAsync(int productId, CancellationToken ct);
    Task<Result<ProductUnitDto>> AddUnitAsync(int productId, AddProductUnitRequest req, CancellationToken ct);
    Task<Result<ProductUnitDto>> UpdateUnitAsync(int productId, int unitId, UpdateProductUnitRequest req, CancellationToken ct);
    Task<Result> DeleteUnitAsync(int productId, int unitId, DeleteStrategy strategy, CancellationToken ct);
    Task<Result<BarcodeResolutionDto>> ResolveBarcodeAsync(string barcode, CancellationToken ct);
    Task<Result<List<ProductPriceHistoryDto>>> GetPriceHistoryAsync(int productId, CancellationToken ct);
}
