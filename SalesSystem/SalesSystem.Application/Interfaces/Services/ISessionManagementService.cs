using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISessionManagementService
{
    Task<Result<List<UserSessionDto>>> GetAllAsync(int? userId, bool includeRevoked, CancellationToken ct);
    Task<Result> RevokeAsync(long sessionId, CancellationToken ct);
}
