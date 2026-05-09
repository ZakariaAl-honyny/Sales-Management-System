using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IPurchaseInvoiceApiService
{
    Task<Result<IReadOnlyList<PurchaseInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest r, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, CreatePurchaseInvoiceRequest r, CancellationToken ct = default);
    Task<Result> PostAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
