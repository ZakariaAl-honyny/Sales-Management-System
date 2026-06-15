namespace SalesSystem.Contracts.Requests;

public record CreateProductCategoryRequest(string Name, int? ParentId);
public record UpdateProductCategoryRequest(string Name);
