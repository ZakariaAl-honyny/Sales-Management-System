using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Categories;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<CategoryDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
