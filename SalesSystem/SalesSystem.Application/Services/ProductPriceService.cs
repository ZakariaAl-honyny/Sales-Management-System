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

    // ─── Effective Price with Fallback Chain (Phase 25) ────

    /// <summary>
    /// Gets the effective price for a product unit with the configured fallback chain:
    /// 1. Exact match (ProductUnitId + CurrencyId + PriceLevel + effective date)
    /// 2. Retail fallback (same currency, retail level)
    /// 3. Base currency conversion (same level in base currency → convert via exchange rate)
    /// 4. Returns Failure with Arabic message if nothing found
    /// </summary>
    Task<Result<EffectivePriceDto>> GetEffectivePriceAsync(
        int productUnitId,
        int currencyId,
        PriceLevel priceLevel,
        DateTime? effectiveDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience method for invoice line pricing — uses UtcNow as effective date.
    /// </summary>
    Task<Result<EffectivePriceDto>> GetEffectivePriceForInvoiceAsync(
        int productUnitId,
        int currencyId,
        PriceLevel priceLevel,
        CancellationToken ct = default);

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
    /// Creates a new product price entry. Records ProductPriceHistory automatically.
    /// </summary>
    Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Updates an existing product price entry. Records ProductPriceHistory automatically.
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

            // Look up the base currency for this lookup
            var baseCurrency = await _uow.Currencies.FirstOrDefaultAsync(
                c => c.IsBaseCurrency && c.IsActive, ct);
            var currencyId = baseCurrency?.Id ?? 1; // fallback to currency ID 1 if no base currency found

            // Prices are on ProductUnit → ProductPrice (no direct Product → Price FK).
            // Use the fallback chain for reliable lookup.
            var effectiveResult = await GetEffectivePriceAsync(
                baseUnit.Id,
                currencyId,
                priceLevel,
                DateTime.UtcNow,
                ct);

            if (!effectiveResult.IsSuccess)
            {
                _logger.LogWarning("GetPriceByUnitAsync: No {PriceLevel} price found for product {ProductId} unit {UnitId}",
                    priceLevel, productId, baseUnit.Id);
                return Result<decimal>.Failure("لا يوجد سعر محدد لهذا المنتج", ErrorCodes.NotFound);
            }

            _logger.LogInformation(
                "GetPriceByUnitAsync: Product {ProductId}, Level {PriceLevel}, Price = {Price}" +
                (effectiveResult.Value!.IsConverted ? " (converted from {OriginalCurrency})" : ""),
                productId, priceLevel, effectiveResult.Value.Price,
                effectiveResult.Value.OriginalCurrencyCode);

            return Result<decimal>.Success(effectiveResult.Value.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price for product {ProductId}", productId);
            return Result<decimal>.Failure("حدث خطأ أثناء استرجاع السعر");
        }
    }

    // ═══════════════════════════════════════════
    //  Effective Price with Fallback Chain
    // ═══════════════════════════════════════════

    public async Task<Result<EffectivePriceDto>> GetEffectivePriceAsync(
        int productUnitId,
        int currencyId,
        PriceLevel priceLevel,
        DateTime? effectiveDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var date = effectiveDate ?? DateTime.UtcNow;
            _logger.LogDebug(
                "GetEffectivePrice: Unit={ProductUnitId}, Currency={CurrencyId}, Level={PriceLevel}, Date={Date}",
                productUnitId, currencyId, priceLevel, date);

            // ─── Step 1: Exact match ──────────────────────────────────
            var exactPrice = await _uow.ProductPrices.FirstOrDefaultAsync(
                pp => pp.ProductUnitId == productUnitId
                   && pp.CurrencyId == currencyId
                   && pp.PriceLevel == priceLevel
                   && pp.IsActive
                   && pp.EffectiveFrom <= date
                   && (!pp.EffectiveTo.HasValue || pp.EffectiveTo.Value >= date),
                ct, "ProductUnit", "Currency");

            if (exactPrice != null)
            {
                _logger.LogInformation(
                    "Effective price found (exact): Unit={ProductUnitId}, Currency={CurrencyId}, Level={PriceLevel}, Price={Price}",
                    productUnitId, currencyId, priceLevel, exactPrice.Price);
                return Result<EffectivePriceDto>.Success(MapToEffectiveDto(exactPrice));
            }

            // ─── Step 2: Retail fallback (same currency) ──────────────
            if (priceLevel > PriceLevel.Retail)
            {
                var retailPrice = await _uow.ProductPrices.FirstOrDefaultAsync(
                    pp => pp.ProductUnitId == productUnitId
                       && pp.CurrencyId == currencyId
                       && pp.PriceLevel == PriceLevel.Retail
                       && pp.IsActive
                       && pp.EffectiveFrom <= date
                       && (!pp.EffectiveTo.HasValue || pp.EffectiveTo.Value >= date),
                    ct, "ProductUnit", "Currency");

                if (retailPrice != null)
                {
                    var fallbackMsg = $"تم استخدام سعر التجزئة — لا يوجد سعر {GetPriceLevelDisplay(priceLevel)}";
                    _logger.LogInformation(
                        "Effective price found (retail fallback): Unit={ProductUnitId}, Currency={CurrencyId}, Price={Price}. {Message}",
                        productUnitId, currencyId, retailPrice.Price, fallbackMsg);
                    return Result<EffectivePriceDto>.Success(
                        MapToEffectiveDto(retailPrice, fallbackMsg));
                }
            }

            // ─── Step 3: Base currency conversion ────────────────────
            var baseCurrency = await _uow.Currencies.FirstOrDefaultAsync(
                c => c.IsBaseCurrency && c.IsActive, ct);

            if (baseCurrency != null && baseCurrency.Id != currencyId)
            {
                // Try same PriceLevel in base currency first
                var basePriceSameLevel = await _uow.ProductPrices.FirstOrDefaultAsync(
                    pp => pp.ProductUnitId == productUnitId
                       && pp.CurrencyId == baseCurrency.Id
                       && pp.PriceLevel == priceLevel
                       && pp.IsActive
                       && pp.EffectiveFrom <= date
                       && (!pp.EffectiveTo.HasValue || pp.EffectiveTo.Value >= date),
                    ct, "Currency");

                // Fallback to Retail in base currency
                var basePriceSource = basePriceSameLevel
                    ?? await _uow.ProductPrices.FirstOrDefaultAsync(
                        pp => pp.ProductUnitId == productUnitId
                           && pp.CurrencyId == baseCurrency.Id
                           && pp.PriceLevel == PriceLevel.Retail
                           && pp.IsActive
                           && pp.EffectiveFrom <= date
                           && (!pp.EffectiveTo.HasValue || pp.EffectiveTo.Value >= date),
                        ct, "Currency");

                if (basePriceSource != null)
                {
                    // Target currency must have a valid exchange rate
                    var targetCurrency = await _uow.Currencies.GetByIdAsync(currencyId, ct);
                    if (targetCurrency != null && targetCurrency.ExchangeRateToBase > 0)
                    {
                        // Convert: base_price / target_exchange_rate = price in target currency
                        var convertedPrice = Math.Round(
                            basePriceSource.Price / targetCurrency.ExchangeRateToBase, 2);

                        var fallbackMsg = basePriceSameLevel != null
                            ? $"تم التحويل من {baseCurrency.Code} بسعر صرف {targetCurrency.ExchangeRateToBase}"
                            : $"تم التحويل من {baseCurrency.Code} (سعر التجزئة) بسعر صرف {targetCurrency.ExchangeRateToBase}";

                        _logger.LogInformation(
                            "Effective price found (base currency conversion): Unit={ProductUnitId}, " +
                            "Price in {BaseCurrency}={BasePrice}, Converted to {TargetCurrency}={ConvertedPrice}, Rate={Rate}",
                            productUnitId, baseCurrency.Code, basePriceSource.Price,
                            targetCurrency.Code, convertedPrice, targetCurrency.ExchangeRateToBase);

                        return Result<EffectivePriceDto>.Success(new EffectivePriceDto(
                            productUnitId,
                            currencyId,
                            targetCurrency.Code,
                            targetCurrency.Name,
                            priceLevel,
                            convertedPrice,
                            basePriceSource.Price,
                            baseCurrency.Id,
                            baseCurrency.Code,
                            baseCurrency.Name,
                            fallbackMsg));
                    }
                }
            }

            // ─── Step 4: Not found ───────────────────────────────────
            _logger.LogWarning(
                "No effective price found: Unit={ProductUnitId}, Currency={CurrencyId}, Level={PriceLevel}",
                productUnitId, currencyId, priceLevel);
            return Result<EffectivePriceDto>.Failure(
                "لا يوجد سعر محدد لهذه التركيبة — يرجى إدخال السعر يدوياً",
                ErrorCodes.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting effective price for unit {ProductUnitId}, currency {CurrencyId}, level {PriceLevel}",
                productUnitId, currencyId, priceLevel);
            return Result<EffectivePriceDto>.Failure("حدث خطأ أثناء استرجاع السعر الفعال");
        }
    }

    public async Task<Result<EffectivePriceDto>> GetEffectivePriceForInvoiceAsync(
        int productUnitId,
        int currencyId,
        PriceLevel priceLevel,
        CancellationToken ct = default)
    {
        return await GetEffectivePriceAsync(productUnitId, currencyId, priceLevel, DateTime.UtcNow, ct);
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

            // ─── Record Price History ─────────────────────────────
            await RecordPriceHistoryAsync(
                productPrice.ProductUnitId,
                "PriceCreated",
                0,  // no old value
                productPrice.Price,
                $"تم إنشاء سعر {GetPriceLevelDisplay(productPrice.PriceLevel)} بقيمة {productPrice.Price} {currency.Code}",
                userId,
                ct);

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

            var oldPrice = price.Price;
            var oldLevel = price.PriceLevel;

            // Update price, price level, effective from, and effective to via domain method
            price.UpdatePrice(request.Price, request.PriceLevel, request.EffectiveFrom, request.EffectiveTo, userId);

            await _uow.SaveChangesAsync(ct);

            // ─── Record Price History ─────────────────────────────
            var changeReason = oldLevel != request.PriceLevel
                ? $"تحديث من {GetPriceLevelDisplay(oldLevel)} ({oldPrice}) إلى {GetPriceLevelDisplay(request.PriceLevel)} ({request.Price})"
                : $"تحديث السعر من {oldPrice} إلى {request.Price}";

            await RecordPriceHistoryAsync(
                price.ProductUnitId,
                "PriceUpdated",
                oldPrice,
                request.Price,
                changeReason,
                userId,
                ct);

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

    // ═══════════════════════════════════════════
    //  Price History Recording
    // ═══════════════════════════════════════════

    /// <summary>
    /// Records a price change in ProductPriceHistory for audit trail.
    /// </summary>
    private async Task RecordPriceHistoryAsync(
        int productUnitId,
        string changeType,
        decimal oldValue,
        decimal newValue,
        string changeReason,
        int changedByUserId,
        CancellationToken ct)
    {
        try
        {
            var history = ProductPriceHistory.CreateWithDetails(
                productUnitId,
                0, // OldRetailPrice — not tracked at ProductPrice granularity
                0, // NewRetailPrice
                0, // OldWholesalePrice
                0, // NewWholesalePrice
                oldValue,
                newValue,
                changeReason,
                changedByUserId);

            await _uow.ProductPriceHistory.AddAsync(history, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Price history recording is non-critical — log warning, don't fail the operation
            _logger.LogWarning(ex,
                "Failed to record price history for product unit {ProductUnitId}: {ChangeType}",
                productUnitId, changeType);
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

    private static EffectivePriceDto MapToEffectiveDto(ProductPrice price, string? fallbackDescription = null) => new(
        price.ProductUnitId,
        price.CurrencyId,
        price.Currency?.Code,
        price.Currency?.Name,
        price.PriceLevel,
        price.Price,
        null, // OriginalPrice
        null, // OriginalCurrencyId
        null, // OriginalCurrencyCode
        null, // OriginalCurrencyName
        fallbackDescription);

    /// <summary>
    /// Returns the Arabic display name for a price level.
    /// </summary>
    private static string GetPriceLevelDisplay(PriceLevel level) => level switch
    {
        PriceLevel.Retail => "تجزئة",
        PriceLevel.Wholesale => "جملة",
        PriceLevel.VIP => "VIP",
        PriceLevel.Distributor => "موزع",
        _ => "غير معروف"
    };
}
