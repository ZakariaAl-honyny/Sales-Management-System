using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISalesReturnService
{
    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, int userId, CancellationToken ct);
    Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, CancellationToken ct);
}

public interface IPurchaseReturnService
{
    Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, int userId, CancellationToken ct);
    Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<PurchaseReturnDto>>> GetAllAsync(int? supplierId, int page, int pageSize, CancellationToken ct);
}
