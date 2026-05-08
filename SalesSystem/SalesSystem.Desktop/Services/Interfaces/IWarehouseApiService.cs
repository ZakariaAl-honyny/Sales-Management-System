using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IWarehouseApiService
{
    Task<Result<IReadOnlyList<WarehouseDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<WarehouseDto>> CreateAsync(string name, string? code, string? location, bool isDefault, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, string name, string? code, string? location, bool isDefault, bool isActive, CancellationToken ct = default);
}
