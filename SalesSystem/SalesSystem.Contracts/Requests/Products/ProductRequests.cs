namespace SalesSystem.Contracts.Requests.Products;

public record CreateProductRequest(
    string Name,
    string? Code,
    string? Barcode,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description);

public record UpdateProductRequest(
    int Id,
    string Name,
    string? Code,
    string? Barcode,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description,
    bool IsActive);
