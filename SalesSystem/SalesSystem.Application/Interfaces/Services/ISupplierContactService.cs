using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISupplierContactService
{
    Task<Result<List<SupplierContactDto>>> GetAllAsync(int supplierId, CancellationToken ct);
    Task<Result<SupplierContactDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<SupplierContactDto>> CreateAsync(CreateSupplierContactRequest request, int userId, CancellationToken ct);
    Task<Result<SupplierContactDto>> UpdateAsync(int id, UpdateSupplierContactRequest request, int userId, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
