using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IAdditionalFeeService
{
    Task<Result<List<AdditionalFeeDto>>> GetFeesByInvoiceAsync(int purchaseInvoiceId, CancellationToken ct);
    Task<Result<AdditionalFeeDto>> CreateFeeAsync(CreateAdditionalFeeRequest request, int purchaseInvoiceId, CancellationToken ct);
    Task<Result> RemoveFeeAsync(int feeId, CancellationToken ct);
    Task<Result> DistributeFeesAsync(int purchaseInvoiceId, CancellationToken ct);
}
