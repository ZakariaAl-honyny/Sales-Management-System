using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IUnitService
{
    Task<Result<UnitDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<UnitDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<UnitDto>> CreateAsync(CreateUnitRequest request, CancellationToken ct);
    Task<Result<UnitDto>> UpdateAsync(int id, UpdateUnitRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
