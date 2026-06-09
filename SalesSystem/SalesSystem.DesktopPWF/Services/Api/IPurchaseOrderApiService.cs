using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service interface for Purchase Orders
/// </summary>
public interface IPurchaseOrderApiService
{
    Task<Result<List<PurchaseOrderDto>>> GetAllAsync(int? supplierId = null, byte? status = null, string? search = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<List<PurchaseOrderDto>>> GetPendingOrdersAsync(CancellationToken ct = default);
    Task<Result<PurchaseOrderDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<Result<PurchaseOrderDto>> UpdateAsync(int id, UpdatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
