using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISupplierApiService
{
    Task<Result<IReadOnlyList<SupplierDto>>> GetAllAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest r, CancellationToken ct = default);
    Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest r, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
    Task<Result> ReactivateAsync(int id, CancellationToken ct = default);
}

