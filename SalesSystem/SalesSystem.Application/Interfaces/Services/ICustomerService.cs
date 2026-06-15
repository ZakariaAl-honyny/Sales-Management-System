using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<CustomerDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, int userId, CancellationToken ct);
    Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteAsync(int id, int userId, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, int userId, CancellationToken ct);
    Task<Result<PagedResult<CustomerBalanceReportDto>>> GetCustomerBalanceReportAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<Result<PagedResult<CustomerAgingReportDto>>> GetCustomerAgingReportAsync(int page, int pageSize, CancellationToken ct = default);
}
