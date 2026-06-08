namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents an image associated with a product.
/// </summary>
public record ProductImageDto(
    int Id,
    int ProductId,
    string ImagePath,
    bool IsPrimary,
    int SortOrder,
    bool IsActive);
