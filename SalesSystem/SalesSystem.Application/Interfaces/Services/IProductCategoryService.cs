using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IProductCategoryService
{
    Task<Result<List<ProductCategoryDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<ProductCategoryDto>> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct);
    Task<Result<ProductCategoryDto>> UpdateAsync(int id, UpdateProductCategoryRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
    Task<Result> ReactivateAsync(int id, CancellationToken ct);
}
