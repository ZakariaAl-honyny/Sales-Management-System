using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Product service aligned to the new 65-table schema.
/// - Prices via ProductPrices (multi-currency, effective dates)
/// - Cost via InventoryBatches (FIFO/FEFO, weighted average)
/// - ImagePath on Product (single image, no separate ProductImages table)
/// - Expiry tracked via InventoryBatches, not Product
/// - Barcode on Product entity (varchar(50), primary barcode for quick lookup)
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IUnitOfWork uow,
        ISystemSettingsRepository systemSettingsRepo,
        ILogger<ProductService> logger)
    {
        _uow = uow;
        _systemSettingsRepo = systemSettingsRepo;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Id == id, ct, "ProductCategory");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(
        string? search, int? categoryId, int page, int pageSize,
        bool includeInactive = false, CancellationToken ct = default)
    {
        Expression<Func<Product, bool>> predicate = p =>
            (string.IsNullOrWhiteSpace(search) ||
             p.Name.Contains(search) ||
             (p.Barcode != null && p.Barcode.Contains(search))) &&
            (!categoryId.HasValue || p.CategoryId == categoryId.Value);

        var includes = new[] { "ProductCategory" };

        var (items, total) = await _uow.Products.GetPagedAsync(
            predicate, q => q.OrderBy(p => p.Name), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<ProductDto>>.Success(
            PagedResult<ProductDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            // ── AllowDuplicateBarcode check ──────────────────────────────────
            var allowDuplicate = await _systemSettingsRepo.GetBoolAsync("AllowDuplicateBarcode", false, ct);
            if (!allowDuplicate && !string.IsNullOrWhiteSpace(request.Barcode))
            {
                var existing = await _uow.Products.FirstOrDefaultAsync(
                    p => p.Barcode != null && p.Barcode == request.Barcode.Trim(), ct);
                if (existing != null)
                    return Result<ProductDto>.Failure("الباركود موجود مسبقاً لمنتج آخر");
            }

            // ─── Create product entity ────────────────────────────────────
            var product = Product.Create(
                name: request.Name,
                categoryId: request.CategoryId,
                description: request.Description,
                reorderLevel: request.ReorderLevel,
                trackExpiry: request.TrackExpiry,
                imagePath: request.ImagePath,
                barcode: request.Barcode
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

            // ── AllowDuplicateBarcode check ──────────────────────────────────
            var allowDuplicate = await _systemSettingsRepo.GetBoolAsync("AllowDuplicateBarcode", false, ct);
            if (!allowDuplicate && !string.IsNullOrWhiteSpace(request.Barcode))
            {
                var existing = await _uow.Products.FirstOrDefaultAsync(
                    p => p.Barcode != null && p.Barcode == request.Barcode.Trim() && p.Id != id, ct);
                if (existing != null)
                    return Result<ProductDto>.Failure("الباركود موجود مسبقاً لمنتج آخر");
            }

            product.Update(
                name: request.Name,
                categoryId: request.CategoryId,
                description: request.Description,
                reorderLevel: request.ReorderLevel,
                trackExpiry: request.TrackExpiry,
                imagePath: request.ImagePath,
                barcode: request.Barcode
            );

            if (request.IsActive != product.IsActive)
            {
                if (request.IsActive) product.Restore();
                else product.MarkAsDeleted();
            }

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

        var hasSalesItems = await _uow.SalesInvoiceLines.AnyAsync(i => i.ProductId == id, ct);
        if (hasSalesItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات بيع");

        var hasPurchaseItems = await _uow.PurchaseInvoiceLines.AnyAsync(i => i.ProductId == id, ct);
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
            return Result<ProductDto>.Failure("الباركود مطلوب", ErrorCodes.ValidationError);

        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode != null && p.Barcode == barcode.Trim(), ct, "ProductCategory");

        if (product == null)
        {
            _logger.LogWarning("Product not found by barcode: {Barcode}", barcode);
            return Result<ProductDto>.Failure("المنتج غير موجود لهذا الباركود", ErrorCodes.NotFound);
        }

        _logger.LogInformation("Product found by barcode: {Barcode} → {ProductName} (ID: {ProductId})",
            barcode, product.Name, product.Id);
        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UploadImageAsync(int id, byte[] imageBytes, string fileName, CancellationToken ct)
    {
        // Phase 25 simplification: Product has a single ImagePath field.
        // UploadImageAsync saves the file and updates ImagePath on the Product entity.
        throw new NotImplementedException("Use Desktop file upload that updates Product.ImagePath directly");
    }

    public async Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays, CancellationToken ct)
    {
        // Expiry tracking moved to InventoryBatches — query batches directly.
        throw new NotImplementedException("Use InventoryBatchService.GetExpiringBatchesAsync instead");
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Name,
            p.CategoryId,
            p.ProductCategory?.Name,
            p.Description,
            p.ReorderLevel,
            p.TrackExpiry,
            p.ImagePath,
            p.IsActive,
            Barcode: p.Barcode
        );
    }
}
