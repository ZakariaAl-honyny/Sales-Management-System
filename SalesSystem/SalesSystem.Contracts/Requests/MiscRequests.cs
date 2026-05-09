namespace SalesSystem.Contracts.Requests;

public record ReportFilterRequest(
    DateTime? DateFrom, DateTime? DateTo,
    int? CustomerId, int? SupplierId,
    int? WarehouseId, int? ProductId
);

public record UpdateSettingsRequest(
    string StoreName, string? Address, string? Phone,
    string? LogoUrl, string Currency, decimal DefaultTaxRate
);

public record CreateUserRequest(
    string Username, string Password, string FullName, string Role
);
