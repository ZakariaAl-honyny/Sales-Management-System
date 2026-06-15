namespace SalesSystem.Contracts.Responses;

public record ProductCategoryDto(
    int Id,
    string Name,
    int? ParentId,
    string? ParentName,
    bool IsActive
);
