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

        // Look up by Product.Barcode (primary barcode on the product entity)
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode != null && p.Barcode == barcode.Trim(), ct, "ProductCategory");

        if (product == null)
        {
            _logger.LogWarning("Barcode not found: {Barcode}", barcode);
            return Result<BarcodeSearchResult>.Failure("الباركود غير موجود", ErrorCodes.NotFound);
        }

        _logger.LogInformation("Barcode resolved: {Barcode} → {ProductName} (ID: {ProductId})",
            barcode, product.Name, product.Id);

        // BarcodeSearchResult requires a ProductUnitId — lookup the base unit
        var baseUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
            pu => pu.ProductId == product.Id && pu.IsBaseUnit, ct);

        return Result<BarcodeSearchResult>.Success(new BarcodeSearchResult(
            ProductId: product.Id,
            ProductName: product.Name,
            Barcode: product.Barcode,
            ProductUnitId: baseUnit?.Id ?? 0,
            UnitName: baseUnit?.Unit?.Name ?? "",
            ConversionFactor: baseUnit?.Factor ?? 1m,
            IsBaseUnit: true,
            SalesPrice: 0m,
            CurrentStockInBaseUnits: 0m));
    }
}
