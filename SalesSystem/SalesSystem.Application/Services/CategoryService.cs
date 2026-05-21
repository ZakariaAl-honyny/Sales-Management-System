using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnitOfWork uow, ILogger<CategoryService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(id, ct);
        if (category == null)
            return Result<CategoryDto>.Failure("الفئة غير موجودة", ErrorCodes.NotFound);

        return Result<CategoryDto>.Success(MapToDto(category));
    }

    public async Task<Result<PagedResult<CategoryDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _uow.Categories.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<CategoryDto>>.Success(PagedResult<CategoryDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        try
        {
            if (await _uow.Categories.Query().AnyAsync(c => c.Name == request.Name, ct))
                return Result<CategoryDto>.Failure("اسم الفئة مستخدم بالفعل", ErrorCodes.DuplicateCode);

            var category = Category.Create(request.Name, request.Description, null);

            await _uow.Categories.AddAsync(category, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);

            return Result<CategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            return Result<CategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating category");
            return Result<CategoryDto>.Failure("حدث خطأ أثناء إضافة الفئة.");
        }
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await _uow.Categories.Query().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null)
                return Result<CategoryDto>.Failure("الفئة غير موجودة", ErrorCodes.NotFound);

            if (await _uow.Categories.Query().AnyAsync(c => c.Name == request.Name && c.Id != id, ct))
                return Result<CategoryDto>.Failure("اسم الفئة مستخدم بالفعل", ErrorCodes.DuplicateCode);

            category.Update(request.Name, request.Description, null);

            if (request.IsActive && !category.IsActive) category.Restore();
            else if (!request.IsActive && category.IsActive) category.MarkAsDeleted();

            await _uow.Categories.UpdateAsync(category, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);

            return Result<CategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            return Result<CategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category {Id}", id);
            return Result<CategoryDto>.Failure("حدث خطأ أثناء تحديث بيانات الفئة.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(id, ct);
        if (category == null)
            return Result.Failure("الفئة غير موجودة", ErrorCodes.NotFound);

        if (await _uow.Products.Query().AnyAsync(p => p.CategoryId == id, ct))
            return Result.Failure("لا يمكن حذف الفئة لأنها مرتبطة بمنتجات");

        await _uow.Categories.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Category soft-deleted: {CategoryId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var category = await _uow.Categories.Query().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category == null)
            return Result.Failure("الفئة غير موجودة", ErrorCodes.NotFound);

        if (await _uow.Products.Query().AnyAsync(p => p.CategoryId == id, ct))
            return Result.Failure("لا يمكن حذف الفئة نهائياً لأنها مرتبطة بمنتجات");

        await _uow.Categories.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Category permanently deleted: {CategoryId}", id);
        return Result.Success();
    }

    private static CategoryDto MapToDto(Category c)
    {
        return new CategoryDto(c.Id, c.Name, c.Description, c.IsActive);
    }
}
