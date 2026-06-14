using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for SystemLog entities which use a long (bigint) primary key.
/// SystemLog cannot use the standard IGenericRepository&lt;T&gt; because it inherits
/// BaseEntityLong (not BaseEntity), and the GenericRepository&lt;T&gt; is constrained
/// to BaseEntity with int Id.
/// </summary>
public interface ISystemLogRepository
{
    /// <summary>
    /// Adds a new system log entry.
    /// </summary>
    Task<SystemLog> AddAsync(SystemLog entity, CancellationToken ct = default);

    /// <summary>
    /// Queries system logs with filtering and pagination.
    /// </summary>
    Task<(IReadOnlyList<SystemLog> Items, int TotalCount)> GetAllAsync(
        int? level, string? source, string? search,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default);
}
