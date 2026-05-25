using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<CustomerPaymentDto>> CreateCustomerPaymentAsync(CreateCustomerPaymentRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<CustomerPaymentDto>>> GetCustomerPaymentsAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<CustomerPaymentDto>> GetCustomerPaymentByIdAsync(int id, CancellationToken ct);
    Task<Result<CustomerPaymentDto>> UpdateCustomerPaymentAsync(int id, UpdateCustomerPaymentRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteCustomerPaymentAsync(int id, int userId, CancellationToken ct);

    Task<Result<SupplierPaymentDto>> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct);
    Task<Result<PagedResult<SupplierPaymentDto>>> GetSupplierPaymentsAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<SupplierPaymentDto>> GetSupplierPaymentByIdAsync(int id, CancellationToken ct);
    Task<Result<SupplierPaymentDto>> UpdateSupplierPaymentAsync(int id, UpdateSupplierPaymentRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteSupplierPaymentAsync(int id, int userId, CancellationToken ct);
}
