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

        var result = await _uow.UnitBarcodes.FirstOrDefaultAsync(
            b => b.BarcodeValue == normalized,
            ct,
            "ProductUnit", "ProductUnit.Product");

        if (result == null)
        {
            _logger.LogWarning("Barcode not found: {Barcode}", normalized);
            return Result<BarcodeSearchResult>.Failure("الباركود غير موجود", ErrorCodes.NotFound);
        }

        var productUnit = result.ProductUnit;
        return Result<BarcodeSearchResult>.Success(new BarcodeSearchResult(
            productUnit.ProductId,
            productUnit.Product?.Name ?? "",
            result.ProductUnitId,
            productUnit.UnitName,
            productUnit.BaseConversionFactor,
            productUnit.IsBaseUnit,
            productUnit.SalesPrice,
            productUnit.PurchaseCost,
            0m
        ));
    }
}
