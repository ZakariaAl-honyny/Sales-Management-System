using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPurchaseOrderService
{
    Task<Result<PurchaseOrderDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<PurchaseOrderDto>>> GetAllAsync(int? supplierId, int? status, string? search,
        DateTime? from, DateTime? to, int page, int pageSize, bool includeInactive, CancellationToken ct);
    Task<Result<PurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, int userId, CancellationToken ct);
    Task<Result<PurchaseOrderDto>> UpdateAsync(int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken ct);
    Task<Result> CancelAsync(int id, int userId, CancellationToken ct);
    Task<Result<List<PurchaseOrderDto>>> GetPendingOrdersAsync(CancellationToken ct);
}
