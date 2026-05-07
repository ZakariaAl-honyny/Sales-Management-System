using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Customers;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<CustomerDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct);
    Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
