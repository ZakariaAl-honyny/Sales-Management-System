using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISalesReturnService
{
    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, int userId, CancellationToken ct);
    Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<SalesReturnDto>> PostAsync(int id, PostSalesReturnRequest request, int userId, CancellationToken ct);
    Task<Result<SalesReturnDto>> CancelAsync(int id, int userId, CancellationToken ct);
}

public interface IPurchaseReturnService
{
    Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, int userId, CancellationToken ct);
    Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<PurchaseReturnDto>>> GetAllAsync(int? supplierId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<PurchaseReturnDto>> CancelAsync(int id, int userId, CancellationToken ct);
}
