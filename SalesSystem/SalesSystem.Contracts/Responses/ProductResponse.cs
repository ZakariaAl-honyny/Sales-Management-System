namespace SalesSystem.Contracts.Responses;

public record ProductResponse(
    int Id, string Barcode, string Name,
    int? CategoryId, string? CategoryName,
    int? UnitId, string? UnitName,
    decimal PurchasePrice, decimal SalePrice, decimal MinStock,
    string? Description, bool IsActive
);
