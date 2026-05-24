using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, IDocumentSequenceService sequenceService, ILogger<ProductService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Id == id, ct, "Category", "RetailUnit", "WholesaleUnit");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        // Build predicate for the search conditions only (IsActive is handled by query filter)
        var searchVal = search;
        System.Linq.Expressions.Expression<System.Func<Product, bool>> predicate = p =>
            (string.IsNullOrWhiteSpace(searchVal) || p.Name.Contains(searchVal) ||
             (p.Barcode != null && p.Barcode.Contains(searchVal))) &&
            (!categoryId.HasValue || p.CategoryId == categoryId.Value);

        var includes = new[] { "Category", "RetailUnit", "WholesaleUnit" };

        var (items, total) = await _uow.Products.GetPagedAsync(
            predicate, q => q.OrderBy(p => p.Name), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<ProductDto>>.Success(PagedResult<ProductDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            // Normalize barcode: treat empty/whitespace as null to avoid unique index conflicts
            string? barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

            // Validate barcode uniqueness only when a barcode was actually provided
            if (barcode != null)
            {
                if (await _uow.Products.AnyIgnoreFiltersAsync(p => p.Barcode == barcode, ct))
                {
                    _logger.LogWarning("Product creation failed: Duplicate barcode {Barcode} (including inactive)", barcode);
                    return Result<ProductDto>.Failure("باركود المنتج مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateBarcode);
                }
            }

            var product = Product.Create(
                request.Name,
                request.PurchasePrice,
                request.RetailPrice,
                request.WholesalePrice,
                request.ConversionFactor,
                request.MinStock,
                barcode,          // Use normalized barcode (null if empty)
                request.CategoryId,
                request.RetailUnitId,
                request.WholesaleUnitId,
                request.Description,
                null
            );

            await _uow.Products.AddAsync(product, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product created: {ProductName} (ID: {ProductId})", product.Name, product.Id);

            return await GetByIdAsync(product.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while creating product");
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating product");
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating product");
            return Result<ProductDto>.Failure("حدث خطأ غير متوقع أثناء إضافة المنتج.");
        }
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await _uow.Products.FirstOrDefaultIgnoreFiltersAsync(p => p.Id == id, ct);
            if (product == null)
                return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // Normalize barcode: treat empty/whitespace as null to avoid unique index conflicts
            string? barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

            // Validate barcode uniqueness only when a real barcode was provided
            if (barcode != null && barcode != product.Barcode)
            {
                if (await _uow.Products.AnyIgnoreFiltersAsync(p => p.Barcode == barcode && p.Id != id, ct))
                    return Result<ProductDto>.Failure("باركود المنتج مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateBarcode);
            }

            product.Update(
                request.Name,
                request.PurchasePrice,
                request.RetailPrice,
                request.WholesalePrice,
                request.ConversionFactor,
                request.MinStock,
                barcode,          // Use normalized barcode (null if empty)
                request.CategoryId,
                request.RetailUnitId,
                request.WholesaleUnitId,
                request.Description,
                null
            );

            if (request.IsActive != product.IsActive)
            {
                if (request.IsActive) product.Restore();
                else product.MarkAsDeleted();
            }

            await _uow.Products.UpdateAsync(product, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId})", product.Name, product.Id);

            return await GetByIdAsync(product.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while updating product {Id}", id);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating product {Id}", id);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating product {Id}", id);
            return Result<ProductDto>.Failure("حدث خطأ غير متوقع أثناء تحديث بيانات المنتج.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        await _uow.Products.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product soft-deleted: {ProductId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultIgnoreFiltersAsync(p => p.Id == id, ct);
        if (product == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        var hasSalesItems = await _uow.SalesInvoiceItems.AnyAsync(i => i.ProductId == id, ct);
        if (hasSalesItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات بيع");

        var hasPurchaseItems = await _uow.PurchaseInvoiceItems.AnyAsync(i => i.ProductId == id, ct);
        if (hasPurchaseItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات شراء");

        try
        {
            await _uow.Products.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product permanently deleted: {ProductId}", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete product {ProductId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف المنتج نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<ProductDto>.Failure("الباركود مطلوب");

        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode == barcode, ct, "Category", "RetailUnit", "WholesaleUnit");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Barcode,
            p.Name,
            p.CategoryId,
            p.Category?.Name,
            p.UnitId, // Legacy
            p.Unit?.Name, // Legacy
            p.WholesaleUnitId,
            p.WholesaleUnit?.Name,
            p.RetailUnitId,
            p.RetailUnit?.Name,
            p.ConversionFactor,
            p.PurchasePrice,
            p.SalePrice, // Legacy
            p.WholesalePrice,
            p.RetailPrice,
            p.MinStock,
            p.Description,
            p.IsActive
        );
    }
}
