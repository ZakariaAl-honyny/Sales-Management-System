using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SystemLog entities which use a long (bigint) primary key.
/// </summary>
public class SystemLogRepository : ISystemLogRepository
{
    protected readonly SalesDbContext _context;

    public SystemLogRepository(SalesDbContext context)
    {
        _context = context;
    }

    public async Task<SystemLog> AddAsync(SystemLog entity, CancellationToken ct = default)
    {
        await _context.SystemLogs.AddAsync(entity, ct);
        return entity;
    }

    public async Task<(IReadOnlyList<SystemLog> Items, int TotalCount)> GetAllAsync(
        int? level, string? source, string? search,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.SystemLogs.AsQueryable();

        if (level.HasValue)
            query = query.Where(x => x.Level == level.Value);
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(x => x.Source != null && x.Source.Contains(source));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Message.Contains(search) || (x.Exception != null && x.Exception.Contains(search)));
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}
