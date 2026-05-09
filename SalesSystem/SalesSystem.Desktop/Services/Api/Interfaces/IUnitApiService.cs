using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IUnitApiService
{
    Task<Result<IReadOnlyList<UnitDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<UnitDto>> CreateAsync(CreateUnitRequest r, CancellationToken ct = default);
    Task<Result<UnitDto>> UpdateAsync(int id, UpdateUnitRequest r, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

