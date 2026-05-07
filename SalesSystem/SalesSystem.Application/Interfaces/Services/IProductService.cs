using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Products;

namespace SalesSystem.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken ct);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
