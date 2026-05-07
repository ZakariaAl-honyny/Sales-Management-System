using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Common;

namespace SalesSystem.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Uses global query filter for soft delete (IsActive = true).
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
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
        if (entity != null)
        {
            entity.MarkAsDeleted();
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
    
    public IQueryable<T> Query()
    {
        return _context.Set<T>().AsQueryable();
    }
}