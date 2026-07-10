using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Services;

public class SessionManagementService : ISessionManagementService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SessionManagementService> _logger;

    public SessionManagementService(IUnitOfWork uow, ILogger<SessionManagementService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<UserSessionDto>>> GetAllAsync(int? userId, bool includeRevoked, CancellationToken ct)
    {
        try
        {
            List<Domain.Entities.UserSession> sessions;

            if (userId.HasValue)
            {
                if (includeRevoked)
                    sessions = await _uow.UserSessions.ToListAsync(
                        s => s.UserId == userId.Value, null, ct, ignoreQueryFilters: true);
                else
                    sessions = await _uow.UserSessions.ToListAsync(
                        s => s.UserId == userId.Value, ct: ct);
            }
            else
            {
                if (includeRevoked)
                    sessions = await _uow.UserSessions.ToListIgnoreFiltersAsync(ct: ct);
                else
                    sessions = await _uow.UserSessions.ToListAsync(
                        s => !s.IsRevoked, ct: ct);
            }

            var dtos = sessions.Select(MapToDto).OrderByDescending(x => x.CreatedAt).ToList();
            return Result<List<UserSessionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user sessions");
            return Result<List<UserSessionDto>>.Failure("حدث خطأ أثناء جلب جلسات المستخدم.");
        }
    }

    public async Task<Result> RevokeAsync(long sessionId, CancellationToken ct)
    {
        try
        {
            var session = await _uow.UserSessions.GetByIdAsync((int)sessionId, ct);
            if (session == null)
                return Result.Failure("الجلسة غير موجودة", ErrorCodes.NotFound);

            session.Revoke();
            await _uow.UserSessions.UpdateAsync(session, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Session {SessionId} revoked by admin", sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke session {SessionId}", sessionId);
            return Result.Failure("حدث خطأ أثناء إلغاء الجلسة.");
        }
    }

    private static UserSessionDto MapToDto(Domain.Entities.UserSession s)
    {
        return new UserSessionDto(
            Id: s.Id,
            UserId: s.UserId,
            UserName: s.User?.UserName,
            DeviceName: s.DeviceName,
            IpAddress: s.IpAddress,
            CreatedAt: s.CreatedAt,
            LastActivityAt: s.LastActivityAt,
            ExpiresAt: s.ExpiresAt,
            IsRevoked: s.IsRevoked);
    }
}
