namespace SalesSystem.Contracts.Requests;

public record CreateProductRequest(
    string Name,
    int CategoryId,
    string? Description = null,
    decimal ReorderLevel = 0,
    bool TrackExpiry = false,
    string? ImagePath = null,
    string? Barcode = null
);

public record UpdateProductRequest(
    string Name,
    int CategoryId,
    string? Description = null,
    decimal ReorderLevel = 0,
    bool TrackExpiry = false,
    string? ImagePath = null,
    string? Barcode = null,
    bool IsActive = true
);
