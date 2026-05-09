using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ICategoryApiService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest r, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest r, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

