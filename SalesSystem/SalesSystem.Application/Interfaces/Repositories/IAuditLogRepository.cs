using System.Linq.Expressions;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for AuditLog entities which use a long (bigint) primary key.
/// AuditLog cannot use the standard IGenericRepository&lt;T&gt; because it inherits
/// BaseEntityLong (not BaseEntity), and the GenericRepository&lt;T&gt; is constrained
/// to BaseEntity with int Id.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task<AuditLog> AddAsync(AuditLog entity, CancellationToken ct = default);

    /// <summary>
    /// Gets a page of audit logs matching the predicate with ordering and include paths.
    /// </summary>
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<AuditLog, bool>>? predicate,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? orderConfig,
        int page,
        int pageSize,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths);

    /// <summary>
    /// Gets audit logs matching optional predicate with optional query config and include paths.
    /// </summary>
    Task<List<AuditLog>> ToListAsync(
        Expression<Func<AuditLog, bool>>? predicate = null,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? queryConfig = null,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths);

    /// <summary>
    /// Gets all audit logs with optional include paths.
    /// </summary>
    Task<List<AuditLog>> ToListAsync(
        CancellationToken ct = default,
        params string[] includePaths);

    /// <summary>
    /// Gets the first audit log matching the predicate with optional include paths.
    /// </summary>
    Task<AuditLog?> FirstOrDefaultAsync(
        Expression<Func<AuditLog, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths);
}
