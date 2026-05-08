using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISupplierPaymentApiService
{
    Task<Result<IReadOnlyList<SupplierPaymentDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken ct = default);
}
