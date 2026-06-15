namespace SalesSystem.Contracts.Requests;

public record CreateProductRequest(
    string Name,
    int CategoryId,
    string? Description = null,
    string? Barcode = null,
    short? TaxId = null,
    decimal ReorderLevel = 0,
    bool TrackExpiry = false,
    string? ImagePath = null,
    string? Notes = null,
    short? DefaultPurchaseUnitId = null,
    short? DefaultSalesUnitId = null,
    decimal? OpeningQuantity = null,
    decimal? OpeningUnitCost = null,
    DateTime? OpeningExpiryDate = null
);

public record UpdateProductRequest(
    string Name,
    int CategoryId,
    string? Description = null,
    string? Barcode = null,
    short? TaxId = null,
    decimal ReorderLevel = 0,
    bool TrackExpiry = false,
    string? ImagePath = null,
    string? Notes = null,
    short? DefaultPurchaseUnitId = null,
    short? DefaultSalesUnitId = null,
    bool IsActive = true
);
