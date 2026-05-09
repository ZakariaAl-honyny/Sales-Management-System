using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ICustomerPaymentApiService
{
    Task<Result<IReadOnlyList<CustomerPaymentDto>>> GetAllAsync(int? customerId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<CustomerPaymentDto>> CreateAsync(CreateCustomerPaymentRequest r, CancellationToken ct = default);
}

