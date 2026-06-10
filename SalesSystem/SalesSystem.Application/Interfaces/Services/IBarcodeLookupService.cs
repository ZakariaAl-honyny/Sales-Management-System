using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IBarcodeLookupService
{
    Task<Result<BarcodeSearchResult>> LookupByBarcodeAsync(string barcode, CancellationToken ct = default);
}

public record BarcodeSearchResult(
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string UnitName,
    decimal BaseConversionFactor,
    bool IsBaseUnit,
    decimal SalesPrice,
    decimal PurchaseCost,
    decimal CurrentStockInBaseUnits
)
{
    // Phase 25: SalesPrice and PurchaseCost are placeholders (pricing moved to ProductPrices).
    // These fields are kept for backward compatibility with Desktop and will be
    // sourced from ProductPrices in a future update.
}