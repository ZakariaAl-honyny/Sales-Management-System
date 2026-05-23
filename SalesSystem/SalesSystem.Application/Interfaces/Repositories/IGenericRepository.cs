using System.Linq.Expressions;
using SalesSystem.Domain.Common;

namespace SalesSystem.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task SoftDeleteAsync(int id, CancellationToken ct = default);
    Task HardDeleteAsync(int id, CancellationToken ct = default);
    void DeleteRange(IEnumerable<T> entities);
    IQueryable<T> Query();

    // ─── Async query replacements (avoid EF Core extension methods in Application) ───────

    /// <summary>
    /// Finds first entity matching predicate with optional include paths (e.g., "Customer", "Items.Product").
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths);

    /// <summary>
    /// Finds first entity matching predicate ignoring soft-delete filter, with optional include paths.
    /// </summary>
    Task<T?> FirstOrDefaultIgnoreFiltersAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default,
        params string[] includePaths);

    /// <summary>
    /// Gets all entities as a list (no predicate) with optional include paths.
    /// </summary>
    Task<List<T>> ToListAsync(
        CancellationToken ct = default,
        params string[] includePaths);

    /// <summary>
    /// Gets entities matching predicate with optional query config (ordering/paging) and include paths.
    /// The queryConfig lambda receives an IQueryable and may call standard LINQ methods (OrderBy, Skip, Take).
    /// </summary>
    Task<List<T>> ToListAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? queryConfig = null,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths);

    /// <summary>
    /// Gets a page of results and total count matching predicate with ordering and include paths.
    /// </summary>
    Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? orderConfig,
        int page,
        int pageSize,
        CancellationToken ct = default,
        bool ignoreQueryFilters = false,
        params string[] includePaths);

    /// <summary>
    /// Gets all entities ignoring soft-delete filter with optional include paths.
    /// </summary>
    Task<List<T>> ToListIgnoreFiltersAsync(
        CancellationToken ct = default,
        params string[] includePaths);

    /// <summary>
    /// Counts entities matching optional predicate.
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Counts entities matching optional predicate ignoring soft-delete filter.
    /// </summary>
    Task<int> CountIgnoreFiltersAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true if any entity matches the predicate.
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true if any entity matches the predicate ignoring soft-delete filter.
    /// </summary>
    Task<bool> AnyIgnoreFiltersAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);
}