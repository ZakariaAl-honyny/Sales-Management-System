using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AuditLog entities which use a long (bigint) primary key.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    protected readonly SalesDbContext _context;

    public AuditLogRepository(SalesDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> AddAsync(AuditLog entity, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(entity, ct);
        return entity;
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<AuditLog, bool>>? predicate,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? orderConfig,
        int page,
        int pageSize,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths)
    {
        IQueryable<AuditLog> query = ignoreQueryFilters
            ? _context.AuditLogs.IgnoreQueryFilters()
            : _context.AuditLogs;

        foreach (var path in includePaths)
            query = query.Include(path);

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);

        IQueryable<AuditLog> ordered = orderConfig != null ? orderConfig(query) : query;
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<AuditLog>> ToListAsync(
        Expression<Func<AuditLog, bool>>? predicate = null,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? queryConfig = null,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths)
    {
        IQueryable<AuditLog> query = ignoreQueryFilters
            ? _context.AuditLogs.IgnoreQueryFilters()
            : _context.AuditLogs;

        foreach (var path in includePaths)
            query = query.Include(path);

        if (predicate != null)
            query = query.Where(predicate);

        if (queryConfig != null)
            query = queryConfig(query);

        return await query.ToListAsync(ct);
    }

    public async Task<List<AuditLog>> ToListAsync(
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<AuditLog> query = _context.AuditLogs;
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.ToListAsync(ct);
    }

    public async Task<AuditLog?> FirstOrDefaultAsync(
        Expression<Func<AuditLog, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<AuditLog> query = _context.AuditLogs;
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.FirstOrDefaultAsync(predicate, ct);
    }
}
