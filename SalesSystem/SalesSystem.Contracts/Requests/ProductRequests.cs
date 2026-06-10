namespace SalesSystem.Contracts.Requests;

public record CreateProductRequest(
    string Name,
    string? Barcode,
    int? CategoryId,
    decimal MinStock,
    string? Description
);

public record UpdateProductRequest(
    string Name,
    string? Barcode,
    int? CategoryId,
    decimal MinStock,
    string? Description,
    bool IsActive
);
