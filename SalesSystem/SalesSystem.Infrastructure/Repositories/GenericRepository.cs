using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Domain.Common;

namespace SalesSystem.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Uses entity-specific query filters configured via Fluent API.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : Entity
{
    protected readonly SalesDbContext _context;

    public GenericRepository(SalesDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Set<T>().FindAsync([id], ct);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<T>().ToListAsync(ct);
    }

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _context.Set<T>().AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.Set<T>().FindAsync([id], ct);
        if (entity != null && entity is ActivatableEntity activatable)
        {
            activatable.MarkAsDeleted();
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(e => ((Entity)e).Id == id, ct);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
        }
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
    }

    public IQueryable<T> Query()
    {
        return _context.Set<T>().AsQueryable();
    }

    // ─── Async query methods ─────────────────────────────────────────

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<T> query = _context.Set<T>();
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<T?> FirstOrDefaultIgnoreFiltersAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<T> query = _context.Set<T>().IgnoreQueryFilters();
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<List<T>> ToListAsync(
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<T> query = _context.Set<T>();
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.ToListAsync(ct);
    }

    public async Task<List<T>> ToListAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? queryConfig = null,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths)
    {
        IQueryable<T> query = ignoreQueryFilters ? _context.Set<T>().IgnoreQueryFilters() : _context.Set<T>();
        foreach (var path in includePaths)
            query = query.Include(path);
        if (predicate != null)
            query = query.Where(predicate);
        if (queryConfig != null)
            query = queryConfig(query);
        return await query.ToListAsync(ct);
    }

    public async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? orderConfig,
        int page,
        int pageSize,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths)
    {
        IQueryable<T> query = ignoreQueryFilters ? _context.Set<T>().IgnoreQueryFilters() : _context.Set<T>();
        foreach (var path in includePaths)
            query = query.Include(path);
        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);

        IQueryable<T> ordered = orderConfig != null ? orderConfig(query) : query;
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<T>> ToListIgnoreFiltersAsync(
        CancellationToken ct = default,
        params string[] includePaths)
    {
        IQueryable<T> query = _context.Set<T>().IgnoreQueryFilters();
        foreach (var path in includePaths)
            query = query.Include(path);
        return await query.ToListAsync(ct);
    }

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IQueryable<T> query = _context.Set<T>();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.CountAsync(ct);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _context.Set<T>().AnyAsync(predicate, ct);
    }

    public async Task<bool> AnyIgnoreFiltersAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _context.Set<T>().IgnoreQueryFilters().AnyAsync(predicate, ct);
    }

    public async Task<int> CountIgnoreFiltersAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IQueryable<T> query = _context.Set<T>().IgnoreQueryFilters();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.CountAsync(ct);
    }
}