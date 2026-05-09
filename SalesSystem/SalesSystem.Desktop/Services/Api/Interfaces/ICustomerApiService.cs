using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ICustomerApiService
{
    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest r, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest r, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
    Task<Result> ReactivateAsync(int id, CancellationToken ct = default);
}

