using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<CategoryDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);
}