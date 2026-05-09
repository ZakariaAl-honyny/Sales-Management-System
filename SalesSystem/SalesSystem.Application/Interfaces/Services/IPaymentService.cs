using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<CustomerPaymentDto>> CreateCustomerPaymentAsync(CreateCustomerPaymentRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<CustomerPaymentDto>>> GetCustomerPaymentsAsync(int? customerId, int page, int pageSize, CancellationToken ct);
    
    Task<Result<SupplierPaymentDto>> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<SupplierPaymentDto>>> GetSupplierPaymentsAsync(int? supplierId, int page, int pageSize, CancellationToken ct);
}
