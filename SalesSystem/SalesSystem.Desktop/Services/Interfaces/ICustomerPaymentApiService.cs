using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ICustomerPaymentApiService
{
    Task<Result<IReadOnlyList<CustomerPaymentDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<CustomerPaymentDto>> CreateAsync(CreateCustomerPaymentRequest request, CancellationToken ct = default);
}
