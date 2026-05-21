namespace SalesSystem.Contracts.Requests;

public record ReportFilterRequest(
    DateTime? DateFrom, DateTime? DateTo,
    int? CustomerId, int? SupplierId,
    int? WarehouseId, int? ProductId
);

public record UpdateSettingsRequest(
    string StoreName, string? Address, string? Phone, string? Email,
    string? LogoUrl, string Currency, decimal DefaultTaxRate,
    bool IsTaxEnabled, string? TaxNumber,
    bool EnableStockAlerts, bool AllowNegativeStock, bool AutoUpdatePrices,
    string InvoicePrefix);

public record UpdatePrintSettingsRequest(
    string ThermalPrinterName,
    string A4PrinterName,
    string LogoPath,
    string StoreTaxNumber,
    decimal TaxRate);
