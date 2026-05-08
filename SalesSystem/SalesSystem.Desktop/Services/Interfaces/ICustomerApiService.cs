using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ICustomerApiService
{
    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(string name, string? phone, string? email, string? address, decimal initialBalance, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, string name, string? phone, string? email, string? address, bool isActive, CancellationToken ct = default);
}
