using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class ProductCategoryService : IProductCategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductCategoryService> _logger;

    public ProductCategoryService(IUnitOfWork uow, ILogger<ProductCategoryService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<ProductCategoryDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        try
        {
            var categories = await _uow.ProductCategories.ToListAsync(
                predicate: null,
                queryConfig: null,
                ct: ct,
                ignoreQueryFilters: includeInactive);
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<ProductCategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all product categories");
            return Result<List<ProductCategoryDto>>.Failure("حدث خطأ أثناء استرجاع قائمة التصنيفات");
        }
    }

    public async Task<Result<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var category = await _uow.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null)
                return Result<ProductCategoryDto>.Failure("التصنيف غير موجود", ErrorCodes.NotFound);

            return Result<ProductCategoryDto>.Success(MapToDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product category {Id}", id);
            return Result<ProductCategoryDto>.Failure("حدث خطأ أثناء استرجاع بيانات التصنيف");
        }
    }

    public async Task<Result<ProductCategoryDto>> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct)
    {
        try
        {
            // Check for duplicate name
            var duplicateName = await _uow.ProductCategories.AnyAsync(c => c.Name == request.Name, ct);
            if (duplicateName)
                return Result<ProductCategoryDto>.Failure("اسم التصنيف مستخدم بالفعل", ErrorCodes.DuplicateEntry);

            var category = ProductCategory.Create(name: request.Name);

            await _uow.ProductCategories.AddAsync(category, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product category created: {Name} (ID: {Id})",
                category.Name, category.Id);

            return Result<ProductCategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating product category: {Message}", ex.Message);
            return Result<ProductCategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product category");
            return Result<ProductCategoryDto>.Failure("حدث خطأ أثناء إنشاء التصنيف");
        }
    }

    public async Task<Result<ProductCategoryDto>> UpdateAsync(int id, UpdateProductCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await _uow.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null)
                return Result<ProductCategoryDto>.Failure("التصنيف غير موجود", ErrorCodes.NotFound);

            // Check for duplicate name (excluding current)
            var duplicateName = await _uow.ProductCategories.AnyAsync(c => c.Name == request.Name && c.Id != id, ct);
            if (duplicateName)
                return Result<ProductCategoryDto>.Failure("اسم التصنيف مستخدم بالفعل", ErrorCodes.DuplicateEntry);

            category.Update(request.Name);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product category updated: {Name} (ID: {Id})", category.Name, id);

            return Result<ProductCategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating product category {Id}: {Message}", id, ex.Message);
            return Result<ProductCategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product category {Id}", id);
            return Result<ProductCategoryDto>.Failure("حدث خطأ أثناء تحديث بيانات التصنيف");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var category = await _uow.ProductCategories.GetByIdAsync(id, ct);
            if (category == null)
                return Result.Failure("التصنيف غير موجود", ErrorCodes.NotFound);

            category.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product category deactivated: {Name} (ID: {Id})", category.Name, id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation deactivating product category {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating product category {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط التصنيف");
        }
    }

    public async Task<Result> ReactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var category = await _uow.ProductCategories.FirstOrDefaultIgnoreFiltersAsync(c => c.Id == id, ct);
            if (category == null)
                return Result.Failure("التصنيف غير موجود", ErrorCodes.NotFound);

            category.Restore();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product category reactivated: {Name} (ID: {Id})", category.Name, id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation reactivating product category {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating product category {Id}", id);
            return Result.Failure("حدث خطأ أثناء استعادة التصنيف");
        }
    }

    private static ProductCategoryDto MapToDto(ProductCategory category)
    {
        return new ProductCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive
        );
    }
}
