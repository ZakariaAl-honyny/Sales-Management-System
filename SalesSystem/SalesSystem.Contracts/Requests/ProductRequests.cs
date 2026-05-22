namespace SalesSystem.Contracts.Requests;

public record CreateProductRequest(
    string Barcode, string Name,
    int? CategoryId, int? UnitId, // Legacy
    int? RetailUnitId, int? WholesaleUnitId,
    decimal ConversionFactor,
    decimal PurchasePrice, decimal SalePrice, // Legacy
    decimal RetailPrice, decimal WholesalePrice,
    decimal MinStock, string? Description
);

public record UpdateProductRequest(
    string Barcode, string Name,
    int? CategoryId, int? UnitId, // Legacy
    int? RetailUnitId, int? WholesaleUnitId,
    decimal ConversionFactor,
    decimal PurchasePrice, decimal SalePrice, // Legacy
    decimal RetailPrice, decimal WholesalePrice,
    decimal MinStock, string? Description,
    bool IsActive
);
