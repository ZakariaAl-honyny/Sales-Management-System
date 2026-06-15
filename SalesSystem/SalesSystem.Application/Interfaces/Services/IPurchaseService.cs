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
    Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, UpdatePurchaseInvoiceRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<PurchaseInvoiceDto>>> GetAllAsync(
        int? supplierId, 
        int? status, 
        string? search = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 10, 
        bool includeInactive = false, 
        CancellationToken ct = default);

    /// <summary>رفع مرفق لفاتورة الشراء.</summary>
    Task<Result<string>> UploadAttachmentAsync(int id, string base64Content, string? fileName, CancellationToken ct);

    /// <summary>حذف مرفق فاتورة الشراء.</summary>
    Task<Result> DeleteAttachmentAsync(int id, CancellationToken ct);
}
