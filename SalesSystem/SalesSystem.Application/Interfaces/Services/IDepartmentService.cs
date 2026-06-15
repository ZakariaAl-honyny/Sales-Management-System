using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IDepartmentService
{
    Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<DepartmentDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct);
    Task<Result<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
