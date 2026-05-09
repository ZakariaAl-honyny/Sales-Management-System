using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPurchaseService
{
    Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, int userId, CancellationToken ct);
    Task<Result<PurchaseInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<PurchaseInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct);
    Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<PurchaseInvoiceDto>>> GetAllAsync(int? supplierId, int? status, int page, int pageSize, CancellationToken ct);
}
