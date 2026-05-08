using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ICategoryApiService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(string name, string? description, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, string name, string? description, bool isActive, CancellationToken ct = default);
}
