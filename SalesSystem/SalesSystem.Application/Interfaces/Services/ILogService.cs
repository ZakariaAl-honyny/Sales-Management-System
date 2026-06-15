using System.Threading;
using System.Threading.Tasks;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for storing client logs (from Desktop app) into the database.
/// Replaces direct IUnitOfWork access from LogsController.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Creates a system log entry.
    /// </summary>
    /// <param name="logLevel">Log level (e.g., Info, Warning, Error, Fatal)</param>
    /// <param name="message">Log message</param>
    /// <param name="exception">Exception details</param>
    /// <param name="stackTrace">Stack trace</param>
    /// <param name="source">Source (e.g., "Desktop")</param>
    /// <param name="context">Context (method/screen name)</param>
    /// <param name="userId">User ID if available</param>
    /// <param name="machineName">Machine name</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result> CreateLogAsync(
        string logLevel,
        string message,
        string? exception,
        string? stackTrace,
        string? source,
        string? context,
        int? userId,
        string? machineName,
        CancellationToken ct = default);

    /// <summary>
    /// Queries system logs with filtering and pagination.
    /// </summary>
    Task<Result<PagedResult<SystemLogDto>>> QueryLogsAsync(
        int? level, string? source, string? search,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default);
}
