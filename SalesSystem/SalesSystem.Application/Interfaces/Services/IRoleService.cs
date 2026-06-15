using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IRoleService
{
    Task<Result<RoleDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);
}
