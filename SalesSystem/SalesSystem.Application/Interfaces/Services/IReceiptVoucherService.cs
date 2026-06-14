using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IReceiptVoucherService
{
    Task<Result<ReceiptVoucherDto>> CreateAsync(CreateReceiptVoucherRequest request, int userId, CancellationToken ct);
    Task<Result<ReceiptVoucherDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<ReceiptVoucherDto>>> GetAllAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<ReceiptVoucherDto>> UpdateAsync(int id, UpdateReceiptVoucherRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<ReceiptVoucherDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<ReceiptVoucherDto>> CancelAsync(int id, int userId, CancellationToken ct);
}
