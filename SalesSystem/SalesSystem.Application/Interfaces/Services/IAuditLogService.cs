using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for recording and querying audit log entries.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Records an audit log entry.
    /// When autoSave is true (default), immediately persists the entry via SaveChangesAsync.
    /// When autoSave is false, the caller is responsible for calling SaveChangesAsync.
    /// Use autoSave=false when the audit log is part of a larger transaction managed by the caller.
    /// </summary>
    Task<Result> LogAsync(int? userId, string action, string entityType,
        int? entityId = null, string? details = null, string? ipAddress = null,
        CancellationToken ct = default, bool autoSave = true);

    /// <summary>
    /// Queries audit logs with optional filters and pagination.
    /// </summary>
    Task<Result<PaginatedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets the recent audit history for a specific user.
    /// </summary>
    Task<Result<IReadOnlyList<AuditLogDto>>> GetUserHistoryAsync(int userId, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets the recent login history, optionally filtered by user.
    /// </summary>
    Task<Result<IReadOnlyList<AuditLogDto>>> GetLoginHistoryAsync(int? userId = null, int limit = 50, CancellationToken ct = default);
}
