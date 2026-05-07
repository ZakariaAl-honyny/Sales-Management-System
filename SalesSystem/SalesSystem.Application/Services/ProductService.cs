using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Products;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, ILogger<ProductService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Unit)
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
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            if (await _uow.Products.Query().AnyAsync(p => p.Code == request.Code, ct))
                return Result<ProductDto>.Failure("كود المنتج مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            if (await _uow.Products.Query().AnyAsync(p => p.Barcode == request.Barcode, ct))
                return Result<ProductDto>.Failure("باركود المنتج مستخدم بالفعل", ErrorCodes.DuplicateBarcode);
        }

        var product = Product.Create(
            request.Name,
            request.PurchasePrice,
            request.SalePrice,
            request.MinStock,
            request.Code,
            request.Barcode,
            request.CategoryId,
            request.UnitId,
            request.Description,
            null
        );

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product created: {ProductName} (ID: {ProductId})", product.Name, product.Id);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != product.Code)
        {
            if (await _uow.Products.Query().AnyAsync(p => p.Code == request.Code && p.Id != id, ct))
                return Result<ProductDto>.Failure("كود المنتج مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode) && request.Barcode != product.Barcode)
        {
            if (await _uow.Products.Query().AnyAsync(p => p.Barcode == request.Barcode && p.Id != id, ct))
                return Result<ProductDto>.Failure("باركود المنتج مستخدم بالفعل", ErrorCodes.DuplicateBarcode);
        }

        product.Update(
            request.Name,
            request.PurchasePrice,
            request.SalePrice,
            request.MinStock,
            request.Code,
            request.Barcode,
            request.CategoryId,
            request.UnitId,
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

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Code,
            p.Barcode,
            p.Name,
            p.CategoryId,
            p.Category?.Name,
            p.UnitId,
            p.Unit?.Name,
            p.PurchasePrice,
            p.SalePrice,
            p.MinStock,
            p.Description,
            p.IsActive
        );
    }
}
