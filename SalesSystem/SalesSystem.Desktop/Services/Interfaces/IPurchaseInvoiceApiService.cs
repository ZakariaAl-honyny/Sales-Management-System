using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Purchases;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IPurchaseInvoiceApiService
{
    Task<Result<IReadOnlyList<PurchaseInvoiceDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default);
    Task<Result> PostAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
