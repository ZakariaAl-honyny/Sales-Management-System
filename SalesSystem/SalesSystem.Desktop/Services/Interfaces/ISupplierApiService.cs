using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISupplierApiService
{
    Task<Result<IReadOnlyList<SupplierDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SupplierDto>> CreateAsync(string name, string? phone, string? email, string? address, decimal initialBalance, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, string name, string? phone, string? email, string? address, bool isActive, CancellationToken ct = default);
}
