using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service interface for Receipt Vouchers (سندات قبض).
/// Maps to ReceiptVouchersController at api/v1/receipt-vouchers.
/// </summary>
public interface IReceiptVoucherApiService
{
    Task<Result<PagedResult<ReceiptVoucherDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> CreateAsync(CreateReceiptVoucherRequest request, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> UpdateAsync(int id, UpdateReceiptVoucherRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> CancelAsync(int id, CancellationToken ct = default);
}
