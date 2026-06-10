using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(IUnitOfWork uow, ILogger<BarcodeLookupService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<BarcodeSearchResult>> LookupByBarcodeAsync(
        string barcode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<BarcodeSearchResult>.Failure("الباركود مطلوب", ErrorCodes.ValidationError);

        var normalized = barcode.Trim().ToUpperInvariant();

        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode == normalized,
            ct,
            "Units", "Category");

        if (product == null)
        {
            _logger.LogWarning("Barcode not found: {Barcode}", normalized);
            return Result<BarcodeSearchResult>.Failure("الباركود غير موجود", ErrorCodes.NotFound);
        }

        var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit);
        if (baseUnit == null)
        {
            _logger.LogWarning("Product {ProductId} has no base unit", product.Id);
            return Result<BarcodeSearchResult>.Failure("المنتج لا يحتوي على وحدة أساسية", ErrorCodes.NotFound);
        }

        // Phase 25: UnitName replaced by UnitId+Unit navigation property;
        // SalesPrice and PurchaseCost removed from ProductUnit (pricing in ProductPrices table).
        return Result<BarcodeSearchResult>.Success(new BarcodeSearchResult(
            product.Id,
            product.Name,
            baseUnit.Id,
            baseUnit.Unit?.Name ?? "غير معروف",
            baseUnit.BaseConversionFactor,
            baseUnit.IsBaseUnit,
            product.Cost,
            product.Cost,
            0m
        ));
    }
}
