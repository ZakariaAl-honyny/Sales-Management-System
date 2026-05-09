namespace SalesSystem.Contracts.Requests;

public record CreateProductRequest(
    string Code, string Barcode, string Name,
    int CategoryId, int UnitId,
    decimal PurchasePrice, decimal SalePrice,
    decimal MinStock, string? Description
);

public record UpdateProductRequest(
    string Code, string Barcode, string Name,
    int CategoryId, int UnitId,
    decimal PurchasePrice, decimal SalePrice,
    decimal MinStock, string? Description,
    bool IsActive
);
