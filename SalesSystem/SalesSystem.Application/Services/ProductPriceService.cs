using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service to manage product prices — lookup, CRUD, and multi-currency pricing.
/// </summary>
public interface IProductPriceService
{
    /// <summary>
    /// Gets the price for a product based on the unit type (legacy lookup).
    /// </summary>
    Task<Result<decimal>> GetPriceByUnitAsync(int productId, UnitType unitType, CancellationToken ct = default);

    // ─── CRUD Operations (Phase 25) ─────────────────────────

    /// <summary>
    /// Gets all active prices for a specific product unit.
    /// </summary>
    Task<Result<List<ProductPriceDto>>> GetByProductUnitAsync(int productUnitId, CancellationToken ct);

    /// <summary>
    /// Gets a single price by ID.
    /// </summary>
    Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new product price entry.
    /// </summary>
    Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Updates an existing product price entry.
    /// </summary>
    Task<Result<ProductPriceDto>> UpdateAsync(int id, UpdateProductPriceRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Soft-deletes (deactivates) a product price entry.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}

public class ProductPriceService : IProductPriceService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductPriceService> _logger;

    public ProductPriceService(IUnitOfWork uow, ILogger<ProductPriceService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  Legacy Price Lookup
    // ═══════════════════════════════════════════

    public async Task<Result<decimal>> GetPriceByUnitAsync(int productId, UnitType unitType, CancellationToken ct = default)
    {
        try
        {
            // Load product with Units navigation for base unit resolution
            var product = await _uow.Products.FirstOrDefaultAsync(p => p.Id == productId, ct, "Units");

            if (product == null)
            {
                _logger.LogWarning("GetPriceByUnitAsync: Product {ProductId} not found", productId);
                return Result<decimal>.Failure("المنتج غير موجود", ErrorCodes.NotFound);
            }

            var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit);
            if (baseUnit == null)
            {
                _logger.LogWarning("GetPriceByUnitAsync: Product {ProductId} has no base unit defined", productId);
                return Result<decimal>.Failure("لا توجد وحدة أساسية للمنتج");
            }

            var priceLevel = unitType == UnitType.Wholesale ? PriceLevel.Wholesale : PriceLevel.Retail;

            // Prices are on ProductUnit → ProductPrice (no direct Product → Price FK).
            // Query the ProductPrices repository for the base unit's matching price.
            var price = await _uow.ProductPrices.FirstOrDefaultAsync(
                pp => pp.ProductUnitId == baseUnit.Id
                   && pp.PriceLevel == priceLevel
                   && pp.IsActive
                   && pp.EffectiveFrom <= DateTime.UtcNow
                   && (!pp.EffectiveTo.HasValue || pp.EffectiveTo.Value >= DateTime.UtcNow),
                ct);

            if (price == null)
            {
                _logger.LogWarning("GetPriceByUnitAsync: No {PriceLevel} price found for product {ProductId} unit {UnitId}",
                    priceLevel, productId, baseUnit.Id);
                return Result<decimal>.Failure("لا يوجد سعر محدد لهذا المنتج", ErrorCodes.NotFound);
            }

            _logger.LogInformation("GetPriceByUnitAsync: Product {ProductId}, Level {PriceLevel}, Price = {Price}",
                productId, priceLevel, price.Price);

            return Result<decimal>.Success(price.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price for product {ProductId}", productId);
            return Result<decimal>.Failure("حدث خطأ أثناء استرجاع السعر");
        }
    }

    // ═══════════════════════════════════════════
    //  CRUD Operations
    // ═══════════════════════════════════════════

    public async Task<Result<List<ProductPriceDto>>> GetByProductUnitAsync(int productUnitId, CancellationToken ct)
    {
        try
        {
            var prices = await _uow.ProductPrices.ToListAsync(
                p => p.ProductUnitId == productUnitId,
                q => q.OrderByDescending(p => p.EffectiveFrom),
                ct,
                includePaths: new[] { "ProductUnit", "Currency" });

            var dtos = prices.Select(MapToDto).ToList();
            return Result<List<ProductPriceDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prices for product unit {ProductUnitId}", productUnitId);
            return Result<List<ProductPriceDto>>.Failure("حدث خطأ أثناء استرجاع الأسعار");
        }
    }

    public async Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var price = await _uow.ProductPrices.FirstOrDefaultAsync(
                p => p.Id == id, ct, "ProductUnit", "Currency");

            if (price == null)
                return Result<ProductPriceDto>.Failure("السعر غير موجود", ErrorCodes.NotFound);

            return Result<ProductPriceDto>.Success(MapToDto(price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product price {Id}", id);
            return Result<ProductPriceDto>.Failure("حدث خطأ أثناء استرجاع السعر");
        }
    }

    public async Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Validate product unit exists
            var productUnit = await _uow.ProductUnits.GetByIdAsync(request.ProductUnitId, ct);
            if (productUnit == null)
                return Result<ProductPriceDto>.Failure("وحدة المنتج غير موجودة", ErrorCodes.NotFound);

            // Validate currency exists
            var currency = await _uow.Currencies.GetByIdAsync(request.CurrencyId, ct);
            if (currency == null)
                return Result<ProductPriceDto>.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            var productPrice = ProductPrice.Create(
                request.ProductUnitId,
                request.CurrencyId,
                request.PriceLevel,
                request.Price,
                request.EffectiveFrom,
                request.EffectiveTo,
                userId);

            await _uow.ProductPrices.AddAsync(productPrice, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product price created: Unit={ProductUnitId}, Currency={CurrencyId}, Level={PriceLevel}, Price={Price} by User {UserId}",
                request.ProductUnitId, request.CurrencyId, request.PriceLevel, request.Price, userId);

            return await GetByIdAsync(productPrice.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating product price: {Message}", ex.Message);
            return Result<ProductPriceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product price");
            return Result<ProductPriceDto>.Failure("حدث خطأ أثناء إنشاء السعر");
        }
    }

    public async Task<Result<ProductPriceDto>> UpdateAsync(int id, UpdateProductPriceRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var price = await _uow.ProductPrices.GetByIdAsync(id, ct);
            if (price == null)
                return Result<ProductPriceDto>.Failure("السعر غير موجود", ErrorCodes.NotFound);

            // Update price, price level, effective from, and effective to via domain method
            price.UpdatePrice(request.Price, request.PriceLevel, request.EffectiveFrom, request.EffectiveTo, userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product price {Id} updated: Price={Price}, Level={PriceLevel} by User {UserId}",
                id, request.Price, request.PriceLevel, userId);

            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating product price {Id}: {Message}", id, ex.Message);
            return Result<ProductPriceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product price {Id}", id);
            return Result<ProductPriceDto>.Failure("حدث خطأ أثناء تحديث السعر");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var price = await _uow.ProductPrices.GetByIdAsync(id, ct);
            if (price == null)
                return Result.Failure("السعر غير موجود", ErrorCodes.NotFound);

            price.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product price {Id} deactivated", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating product price {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط السعر");
        }
    }

    // ─── Mapping ─────────────────────────────────

    private static ProductPriceDto MapToDto(ProductPrice price) => new(
        price.Id,
        price.ProductUnitId,
        price.ProductUnit?.UnitName,
        price.CurrencyId,
        price.Currency?.Code,
        price.Currency?.Name,
        price.PriceLevel,
        price.Price,
        price.EffectiveFrom,
        price.EffectiveTo,
        price.IsActive);
}
