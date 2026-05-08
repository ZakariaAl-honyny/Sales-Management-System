using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IUnitApiService
{
    Task<Result<IReadOnlyList<UnitDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<UnitDto>> CreateAsync(string name, string? symbol, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, string name, string? symbol, bool isActive, CancellationToken ct = default);
}
