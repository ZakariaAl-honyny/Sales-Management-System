namespace SalesSystem.Contracts.Responses;

public record DashboardSummaryResponse(
    decimal TotalSalesToday,
    decimal TotalPurchasesToday,
    int LowStockItemsCount,
    decimal TotalReceivables,
    decimal TotalPayables,
    int PendingInvoices,
    decimal TotalStockValue,
    List<TopProductResponse> TopProducts
);

public record TopProductResponse(string Name, decimal Quantity);

public record SettingsResponse(
    string StoreName, string? Address, string? Phone,
    string? LogoUrl, string Currency, decimal DefaultTaxRate
);

public record UserResponse(
    int Id, string Username, string FullName, string Role, bool IsActive
);
