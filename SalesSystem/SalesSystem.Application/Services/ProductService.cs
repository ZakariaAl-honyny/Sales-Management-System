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
    private readonly ILogger<ProductService> _logger;
    private readonly ILocalImageStorageService _imageStorage;

    public ProductService(IUnitOfWork uow, ILogger<ProductService> logger, ILocalImageStorageService imageStorage)
    {
        _uow = uow;
        _logger = logger;
        _imageStorage = imageStorage;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Id == id, ct, "Category");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        // Build predicate for the search conditions only (IsActive is handled by query filter)
        var searchVal = search;
        System.Linq.Expressions.Expression<System.Func<Product, bool>> predicate = p =>
            (string.IsNullOrWhiteSpace(searchVal) || p.Name.Contains(searchVal)) &&
            (!categoryId.HasValue || p.CategoryId == categoryId.Value);

        var includes = new[] { "Category" };

        var (items, total) = await _uow.Products.GetPagedAsync(
            predicate, q => q.OrderBy(p => p.Name), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<ProductDto>>.Success(PagedResult<ProductDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            // Phase 25: Barcode is on Product.Barcode (single source of truth)
            // TODO: Create ProductUnit + ProductPrice from request (via separate service calls)

            var product = Product.Create(
                request.Name,
                request.CategoryId,
                request.MinStock,
                0,                              // reorderLevel (default 0)
                false,                          // hasExpiry (expiry tracked per InventoryBatch)
                request.Barcode,
                request.Description,
                null                            // createdByUserId
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

            // Barcode uniqueness validated by Product.Barcode unique constraint in DB

            product.Update(
                request.Name,
                request.CategoryId,
                request.MinStock,
                0,                              // reorderLevel (default 0)
                product.HasExpiry,              // keep existing value
                request.Barcode,
                request.Description,
                null                            // updatedByUserId
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

        // Search Products by Barcode directly (single source of truth)
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode == barcode, ct, "Category");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UploadImageAsync(int id, byte[] imageBytes, string fileName, CancellationToken ct)
    {
        try
        {
            // Step 1: Verify product exists
            var product = await _uow.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product == null)
                return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // Step 2: Save image via infrastructure service (handles validation + path traversal guard)
            var saveResult = await _imageStorage.SaveImageAsync(imageBytes, fileName, id);
            if (!saveResult.IsSuccess)
                return Result<ProductDto>.Failure(saveResult.Error!);

            // Step 3: Update product timestamp only — ImagePath is removed,
            // images are managed via ProductImage entity collection
            product.UpdateTimestamp();

            await _uow.Products.UpdateAsync(product, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Image uploaded for product {ProductId}", id);
            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while uploading image for product {Id}", id);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading image for product {Id}", id);
            return Result<ProductDto>.Failure("حدث خطأ غير متوقع أثناء رفع الصورة.");
        }
    }

    public async Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays, CancellationToken ct)
    {
        if (thresholdDays < 0)
        {
            _logger.LogWarning("GetExpiringProductsAsync called with negative threshold: {ThresholdDays}", thresholdDays);
            return Result<List<ProductDto>>.Failure("عدد الأيام يجب أن يكون صفراً أو أكثر");
        }
        if (thresholdDays > 365)
        {
            _logger.LogWarning("GetExpiringProductsAsync called with excessive threshold: {ThresholdDays}", thresholdDays);
            return Result<List<ProductDto>>.Failure("عدد الأيام لا يمكن أن يتجاوز 365 يوماً");
        }

        try
        {
            var cutoffDate = DateTime.Today.AddDays(thresholdDays);

            // Phase 25: ExpirationDate moved to InventoryBatches entity
            // Get batches that are expiring within threshold days
            var expiringBatches = await _uow.InventoryBatches.ToListAsync(
                b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= cutoffDate,
                q => q.OrderBy(b => b.ExpiryDate),
                ct,
                includePaths: new[] { "Product", "Product.Category" });

            // Group by product to get unique products
            var productIds = expiringBatches.Select(b => b.ProductId).Distinct().ToList();
            var products = await _uow.Products.ToListAsync(
                p => productIds.Contains(p.Id),
                q => q.OrderBy(p => p.Name),
                ct,
                includePaths: new[] { "Category" });

            var dtos = products.Select(MapToDto).ToList();

            _logger.LogInformation("Found {Count} products with batches expiring within {ThresholdDays} days (cutoff: {CutoffDate})",
                dtos.Count, thresholdDays, cutoffDate);

            return Result<List<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expiring products with threshold {ThresholdDays}", thresholdDays);
            return Result<List<ProductDto>>.Failure("حدث خطأ أثناء البحث عن المنتجات المنتهية صلاحيتها.");
        }
    }

    private static ProductDto MapToDto(Product p)
    {
        // Phase 25 restructured data:
        //   - Barcode     → Product.Barcode (single source of truth)
        //   - Prices      → ProductPrices table (ProductUnitId + CurrencyId → Price, no PriceLevel)
        //   - ExpiryDate  → InventoryBatches entity
        //   - Cost     → computed from InventoryBatches via product.UpdateCost()
        // TODO: Load full data from ProductPrices and InventoryBatches for richer DTO.
        return new ProductDto(
            p.Id,
            p.Barcode,
            p.Name,
            p.CategoryId,
            p.Category?.Name,
            p.MinStockLevel,
            p.Description,
            p.HasExpiry,
            p.Cost,
            p.IsActive
        );
    }
}
