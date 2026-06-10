namespace SalesSystem.Contracts.Responses;

public record ProductResponse(
    int Id,
    string Name,
    string? Barcode,
    int? CategoryId,
    string? CategoryName,
    decimal MinStock,
    string? Description,
    bool IsActive
);
