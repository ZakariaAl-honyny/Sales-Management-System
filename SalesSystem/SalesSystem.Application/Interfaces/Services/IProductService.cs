using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);
    Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct);
    Task<Result<ProductDto>> UploadImageAsync(int id, byte[] imageBytes, string fileName, CancellationToken ct);
    Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays, CancellationToken ct);
}
