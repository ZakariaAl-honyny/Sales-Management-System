using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISupplierService
{
    Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<SupplierDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request, CancellationToken ct);
    Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
