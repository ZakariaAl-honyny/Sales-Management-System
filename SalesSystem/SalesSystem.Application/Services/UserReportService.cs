using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Services;

public class UserReportService : IUserReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UserReportService> _logger;

    public UserReportService(IUnitOfWork uow, ILogger<UserReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<UserActivityReportDto>>> GetUserActivityAsync(int? userId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<UserActivityReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting user activity for user {UserId} from {From} to {To}", userId, from, to);

            var auditLogs = await _uow.AuditLogs.ToListAsync(
                al => al.CreatedAt >= from && al.CreatedAt <= to
                   && (!userId.HasValue || al.UserId == userId.Value),
                q => q.OrderByDescending(al => al.CreatedAt),
                ct);

            // Get user names
            var distinctUserIds = auditLogs.Where(al => al.UserId.HasValue).Select(al => al.UserId!.Value).Distinct().ToList();
            var users = distinctUserIds.Count > 0
                ? await _uow.Users.ToListAsync(u => distinctUserIds.Contains(u.Id), ct: ct)
                : new List<Domain.Entities.User>();
            var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

            var result = auditLogs.Select(al =>
            {
                int? parsedEntityId = al.EntityId != null && int.TryParse(al.EntityId, out var eid) ? eid : null;
                return new UserActivityReportDto(
                    al.UserId ?? 0,
                    userDict.GetValueOrDefault(al.UserId ?? 0, "نظام"),
                    al.CreatedAt,
                    al.Action,
                    al.EntityName ?? string.Empty,
                    parsedEntityId,
                    al.Details
                );
            }).ToList();

            return Result<List<UserActivityReportDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user activity report");
            return Result<List<UserActivityReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير نشاط المستخدم");
        }
    }

    public async Task<Result<List<LoginHistoryDto>>> GetLoginHistoryAsync(int? userId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<LoginHistoryDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting login history for user {UserId} from {From} to {To}", userId, from, to);

            // Get audit logs related to login actions
            var loginLogs = await _uow.AuditLogs.ToListAsync(
                al => (al.Action == "LoginSuccess" || al.Action == "LoginFailed" || al.Action == "LoginBlocked_Locked")
                   && al.CreatedAt >= from && al.CreatedAt <= to
                   && (!userId.HasValue || al.UserId == userId.Value),
                q => q.OrderByDescending(al => al.CreatedAt),
                ct);

            var distinctUserIds = loginLogs.Where(al => al.UserId.HasValue).Select(al => al.UserId!.Value).Distinct().ToList();
            var users = distinctUserIds.Count > 0
                ? await _uow.Users.ToListAsync(u => distinctUserIds.Contains(u.Id), ct: ct)
                : new List<Domain.Entities.User>();
            var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

            var result = loginLogs.Select(al => new LoginHistoryDto(
                al.UserId ?? 0,
                userDict.GetValueOrDefault(al.UserId ?? 0, "نظام"),
                al.CreatedAt,
                al.Action == "LoginSuccess",
                al.Action == "LoginSuccess" ? null : (al.Details ?? al.Action)
            )).ToList();

            return Result<List<LoginHistoryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating login history");
            return Result<List<LoginHistoryDto>>.Failure("حدث خطأ أثناء إنشاء سجل تسجيل الدخول");
        }
    }

    public async Task<Result<List<AuditTrailSummaryDto>>> GetAuditTrailSummaryAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<AuditTrailSummaryDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting audit trail summary from {From} to {To}", from, to);

            var auditLogs = await _uow.AuditLogs.ToListAsync(
                al => al.CreatedAt >= from && al.CreatedAt <= to,
                q => q.OrderByDescending(al => al.CreatedAt),
                ct);

            var distinctUserIds = auditLogs.Where(al => al.UserId.HasValue).Select(al => al.UserId!.Value).Distinct().ToList();
            var users = distinctUserIds.Count > 0
                ? await _uow.Users.ToListAsync(u => distinctUserIds.Contains(u.Id), ct: ct)
                : new List<Domain.Entities.User>();
            var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

            var result = auditLogs.Select(al => new AuditTrailSummaryDto(
                al.CreatedAt,
                userDict.GetValueOrDefault(al.UserId ?? 0, "نظام"),
                al.Action,
                al.EntityName ?? string.Empty,
                al.EntityId,
                al.Details
            )).ToList();

            return Result<List<AuditTrailSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit trail summary");
            return Result<List<AuditTrailSummaryDto>>.Failure("حدث خطأ أثناء إنشاء ملخص سجل التدقيق");
        }
    }
}
