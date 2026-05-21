using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);
}
