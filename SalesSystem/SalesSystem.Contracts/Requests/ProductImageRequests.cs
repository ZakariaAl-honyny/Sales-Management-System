namespace SalesSystem.Contracts.Requests;

public record CreateProductImageRequest(
    int ProductId,
    string ImagePath,
    bool IsPrimary = false,
    int SortOrder = 0);

public record SetPrimaryImageRequest(
    int ProductId,
    int ImageId);
