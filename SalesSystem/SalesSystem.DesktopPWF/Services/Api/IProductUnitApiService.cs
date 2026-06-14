using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IProductUnitApiService
{
    Task<Result<List<ProductUnitDto>>> GetByProductIdAsync(int productId);
    Task<Result<ProductUnitDto>> AddUnitAsync(int productId, AddProductUnitRequest request);
    Task<Result<ProductUnitDto>> UpdateUnitAsync(int productId, int unitId, UpdateProductUnitRequest request);
    Task<Result> DeleteUnitAsync(int productId, int unitId, Contracts.Enums.DeleteStrategy strategy);
    Task<Result<BarcodeResolutionDto>> ResolveBarCodeAsync(string barcode);
}
