using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IEmployeeService
{
    Task<Result<List<EmployeeDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<EmployeeDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct);
    Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
