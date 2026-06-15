using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service for recording and querying audit log entries using the AuditLog entity.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IUnitOfWork uow, ILogger<AuditLogService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> LogAsync(int? userId, string action, string entityType,
        int? entityId = null, string? details = null, string? ipAddress = null,
        CancellationToken ct = default, bool autoSave = true)
    {
        try
        {
            _logger.LogInformation("Audit log: User {UserId} performed {Action} on {EntityType}#{EntityId}",
                userId, action, entityType, entityId);

            var auditLog = AuditLog.Create(userId, action, entityType,
                entityId?.ToString(), details, ipAddress);

            await _uow.AuditLogs.AddAsync(auditLog, ct);

            if (autoSave)
                await _uow.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit log");
            return Result.Failure("حدث خطأ أثناء تسجيل السجل.");
        }
    }

    public async Task<Result<PaginatedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        try
        {
            var predicate = BuildPredicate(query);

            var (items, totalCount) = await _uow.AuditLogs.GetPagedAsync(
                predicate,
                orderConfig: q => q.OrderByDescending(l => l.Id),
                query.Page,
                query.PageSize,
                ct: ct,
                includePaths: new[] { "User" });

            var dtos = items.Select(MapToDto).ToList();

            var result = new PaginatedResult<AuditLogDto>(dtos, totalCount, query.Page, query.PageSize);
            return Result<PaginatedResult<AuditLogDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit logs");
            return Result<PaginatedResult<AuditLogDto>>.Failure("حدث خطأ أثناء جلب سجلات المراجعة.");
        }
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> GetUserHistoryAsync(int userId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var items = await _uow.AuditLogs.ToListAsync(
                l => l.UserId == userId,
                q => q.OrderByDescending(l => l.Id),
                ct: ct,
                includePaths: new[] { "User" });

            var dtos = items.Take(limit).Select(MapToDto).ToList();
            return Result<IReadOnlyList<AuditLogDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user history for user {UserId}", userId);
            return Result<IReadOnlyList<AuditLogDto>>.Failure("حدث خطأ أثناء جلب سجل المستخدم.");
        }
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> GetLoginHistoryAsync(int? userId = null, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            List<AuditLog> items;

            if (userId.HasValue)
            {
                items = await _uow.AuditLogs.ToListAsync(
                    l => l.UserId == userId &&
                         (l.Action == "LoginSuccess" || l.Action == "LoginFailed"),
                    q => q.OrderByDescending(l => l.Id),
                    ct: ct,
                    includePaths: new[] { "User" });
            }
            else
            {
                items = await _uow.AuditLogs.ToListAsync(
                    l => l.Action == "LoginSuccess" || l.Action == "LoginFailed",
                    q => q.OrderByDescending(l => l.Id),
                    ct: ct,
                    includePaths: new[] { "User" });
            }

            var dtos = items.Take(limit).Select(MapToDto).ToList();
            return Result<IReadOnlyList<AuditLogDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login history");
            return Result<IReadOnlyList<AuditLogDto>>.Failure("حدث خطأ أثناء جلب سجل تسجيل الدخول.");
        }
    }

    private static System.Linq.Expressions.Expression<Func<AuditLog, bool>>? BuildPredicate(AuditLogQuery query)
    {
        if (!query.UserId.HasValue && string.IsNullOrWhiteSpace(query.Action) &&
            string.IsNullOrWhiteSpace(query.EntityType) && !query.From.HasValue && !query.To.HasValue)
            return null;

        System.Linq.Expressions.Expression<Func<AuditLog, bool>>? predicate = null;

        if (query.UserId.HasValue)
        {
            int uid = query.UserId.Value;
            predicate = l => l.UserId == uid;
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action;
            predicate = predicate != null
                ? Combine(predicate, l => l.Action.Contains(action))
                : l => l.Action.Contains(action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType;
            predicate = predicate != null
                ? Combine(predicate, l => (l.EntityName ?? "").Contains(entityType))
                : l => (l.EntityName ?? "").Contains(entityType);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value;
            predicate = predicate != null
                ? Combine(predicate, l => l.CreatedAt >= from)
                : l => l.CreatedAt >= from;
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value;
            predicate = predicate != null
                ? Combine(predicate, l => l.CreatedAt <= to)
                : l => l.CreatedAt <= to;
        }

        return predicate;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> Combine<T>(
        System.Linq.Expressions.Expression<Func<T, bool>> left,
        System.Linq.Expressions.Expression<Func<T, bool>> right)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T));
        var body = System.Linq.Expressions.Expression.AndAlso(
            System.Linq.Expressions.Expression.Invoke(left, parameter),
            System.Linq.Expressions.Expression.Invoke(right, parameter));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto(
            Id: log.Id,
            UserId: log.UserId,
            UserName: log.User?.UserName,
            Action: log.Action,
            EntityType: log.EntityName,
            EntityId: log.EntityId,
            Details: log.Details,
            IpAddress: log.IpAddress,
            Timestamp: log.CreatedAt
        );
    }
}
