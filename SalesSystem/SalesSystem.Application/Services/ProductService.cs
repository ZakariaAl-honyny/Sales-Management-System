using Microsoft.EntityFrameworkCore;
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
        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.RetailUnit)
            .Include(p => p.WholesaleUnit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _uow.Products.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        query = query.Include(p => p.Category)
                     .Include(p => p.RetailUnit)
                     .Include(p => p.WholesaleUnit)
                     .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) ||
                                    (p.Code != null && p.Code.Contains(search)) ||
                                    (p.Barcode != null && p.Barcode.Contains(search)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<ProductDto>>.Success(PagedResult<ProductDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            string? code = request.Code;
            if (string.IsNullOrWhiteSpace(code))
            {
                var codeResult = await _sequenceService.GetNextNumberAsync("PRD", ct);
                if (!codeResult.IsSuccess)
                    return Result<ProductDto>.Failure(codeResult.Error ?? "حدث خطأ أثناء توليد الكود");
                code = codeResult.Value;
            }

            // Normalize barcode: treat empty/whitespace as null to avoid unique index conflicts
            string? barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

            // Validate code uniqueness (including inactive products)
            if (await _uow.Products.Query().IgnoreQueryFilters().AnyAsync(p => p.Code == code, ct))
            {
                _logger.LogWarning("Product creation failed: Duplicate code {Code} (including inactive)", code);
                return Result<ProductDto>.Failure("كود المنتج مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            // Validate barcode uniqueness only when a barcode was actually provided
            if (barcode != null)
            {
                if (await _uow.Products.Query().IgnoreQueryFilters().AnyAsync(p => p.Barcode == barcode, ct))
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
                code,
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
            var product = await _uow.Products.Query().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product == null)
                return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // Normalize barcode: treat empty/whitespace as null to avoid unique index conflicts
            string? barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != product.Code)
            {
                if (await _uow.Products.Query().IgnoreQueryFilters().AnyAsync(p => p.Code == request.Code && p.Id != id, ct))
                    return Result<ProductDto>.Failure("كود المنتج مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            // Validate barcode uniqueness only when a real barcode was provided
            if (barcode != null && barcode != product.Barcode)
            {
                if (await _uow.Products.Query().IgnoreQueryFilters().AnyAsync(p => p.Barcode == barcode && p.Id != id, ct))
                    return Result<ProductDto>.Failure("باركود المنتج مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateBarcode);
            }

            product.Update(
                request.Name,
                request.PurchasePrice,
                request.RetailPrice,
                request.WholesalePrice,
                request.ConversionFactor,
                request.MinStock,
                request.Code,
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
        var product = await _uow.Products.Query().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        var hasSalesItems = await _uow.SalesInvoiceItems.Query().AnyAsync(i => i.ProductId == id, ct);
        if (hasSalesItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات بيع");

        var hasPurchaseItems = await _uow.PurchaseInvoiceItems.Query().AnyAsync(i => i.ProductId == id, ct);
        if (hasPurchaseItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات شراء");

        await _uow.Products.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product permanently deleted: {ProductId}", id);
        return Result.Success();
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<ProductDto>.Failure("الباركود مطلوب");

        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.RetailUnit)
            .Include(p => p.WholesaleUnit)
            .FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Code,
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
