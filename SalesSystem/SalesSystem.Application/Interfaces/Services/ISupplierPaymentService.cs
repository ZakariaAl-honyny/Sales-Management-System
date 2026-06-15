using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISupplierPaymentService
{
    Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<SupplierPaymentDto>>> GetAllAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<SupplierPaymentDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<SupplierPaymentDto>> UpdateAsync(int id, UpdateSupplierPaymentRequest request, int userId, CancellationToken ct);
    Task<Result> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result> CancelAsync(int id, int userId, CancellationToken ct);
    Task<Result> DeleteAsync(int id, int userId, CancellationToken ct);
}
