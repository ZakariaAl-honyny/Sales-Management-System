using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPaymentVoucherService
{
    Task<Result<PaymentVoucherDto>> CreateAsync(CreatePaymentVoucherRequest request, int userId, CancellationToken ct);
    Task<Result<PaymentVoucherDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<PaymentVoucherDto>>> GetAllAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<PaymentVoucherDto>> UpdateAsync(int id, UpdatePaymentVoucherRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<PaymentVoucherDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<PaymentVoucherDto>> CancelAsync(int id, int userId, CancellationToken ct);
}
