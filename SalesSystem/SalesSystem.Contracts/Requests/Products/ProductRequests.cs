namespace SalesSystem.Contracts.Requests.Products;

public record CreateProductRequest(
    string? Code,
    string? Barcode,
    string Name,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description);

public record UpdateProductRequest(
    int Id,
    string? Code,
    string? Barcode,
    string Name,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description);