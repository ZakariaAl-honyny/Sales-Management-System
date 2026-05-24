namespace SalesSystem.Contracts.DTOs;

public record UserDto(int Id, string UserName, string FullName, byte Role, bool IsActive);

public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);

public record CategoryDto(int Id, string Name, string? Description, bool IsActive);

public record ProductDto(
    int Id,
    string? Barcode,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int? UnitId, // Legacy
    string? UnitName, // Legacy
    int? WholesaleUnitId,
    string? WholesaleUnitName,
    int? RetailUnitId,
    string? RetailUnitName,
    decimal ConversionFactor,
    decimal PurchasePrice,
    decimal SalePrice, // Legacy
    decimal WholesalePrice,
    decimal RetailPrice,
    decimal MinStock,
    string? Description,
    bool IsActive);

public record WarehouseDto(int Id, string Name, string? Location, bool IsDefault, bool IsActive);

public record WarehouseStockDto(
    int WarehouseId,
    string? WarehouseName,
    int ProductId,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal ReorderLevel);

public record SupplierDto(int Id, string Name, string? Phone, string? Email, string? Address, string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance, decimal CreditLimit, bool IsActive);

public record CustomerDto(int Id, string Name, string? Phone, string? Email, string? Address, string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance, decimal CreditLimit, bool IsActive)
{
    public bool IsBalanceNegative 
    { 
        get => CurrentBalance > 0; 
    }
}

public record SalesInvoiceDto(
    int Id,
    string InvoiceNo,
    int? CustomerId,
    string? CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    DateOnly? DueDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string? Notes,
    byte Status,
    IReadOnlyList<SalesInvoiceItemDto> Items)
{
    public string PaymentTypeDisplay => PaymentType switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };

    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record SalesInvoiceItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record PurchaseInvoiceDto(
    int Id,
    string InvoiceNo,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    DateOnly? DueDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string? SupplierInvoiceNo,
    string? Notes,
    byte Status,
    IReadOnlyList<PurchaseInvoiceItemDto> Items)
{
    public string PaymentTypeDisplay => PaymentType switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };

    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record PurchaseInvoiceItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record SalesReturnDto(
    int Id,
    string ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int? CustomerId,
    string CustomerName,
    int? SalesInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? Notes,
    byte Status, IReadOnlyList<SalesReturnItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record SalesReturnItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record PurchaseReturnDto(
    int Id,
    string ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int SupplierId,
    string SupplierName,
    int? PurchaseInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? Notes,
    byte Status, IReadOnlyList<PurchaseReturnItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record PurchaseReturnItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record StockTransferDto(
    int Id,
    string TransferNo,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    DateTime TransferDate,
    string? Notes,
    byte Status, IReadOnlyList<StockTransferItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record StockTransferItemDto(int Id, int ProductId, string ProductName, decimal Quantity, byte Mode, string? Notes);

public record CustomerPaymentDto(
    int Id,
    string PaymentNo,
    int CustomerId,
    string CustomerName,
    decimal Amount,
    byte PaymentMethod,
    DateTime PaymentDate,
    int? SalesInvoiceId,
    string? Notes)
{
    public string PaymentTypeDisplay => PaymentMethod switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };
}

public record SupplierPaymentDto(
    int Id,
    string PaymentNo,
    int SupplierId,
    string SupplierName,
    decimal Amount,
    byte PaymentMethod,
    DateTime PaymentDate,
    int? PurchaseInvoiceId,
    string? Notes)
{
    public string PaymentTypeDisplay => PaymentMethod switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };
}

public record InventoryMovementDto(
    long Id,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    byte MovementType,
    decimal QuantityChange,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string ReferenceType,
    int ReferenceId,
    DateTime MovementDate,
    string? Notes);

public record PrintSettingsDto(
    string ThermalPrinterName,
    string A4PrinterName,
    string LogoPath,
    string StoreTaxNumber,
    decimal TaxRate,
    bool AutoPrintOnPost,
    string ReceiptHeader,
    string ReceiptFooter,
    int EscPosCodePage);

public record StoreSettingsDto(
    int Id,
    string StoreName,
    string? Phone,
    string? Address,
    string? LogoPath,
    string? Email,
    string CurrencyCode,
    decimal DefaultTaxRate,
    bool IsTaxEnabled,
    string? TaxNumber,
    bool EnableStockAlerts,
    bool AllowNegativeStock,
    bool AutoUpdatePrices,
    string InvoicePrefix,
    int CostingMethod = 1,
    string? BackupPath = null,
    string? BackupScheduleTime = "02:00",
    int BackupRetentionDays = 30,
    string? UpdateServerUrl = null);

public record DocumentSequenceDto(int Id, string DocumentType, string Prefix, int Year, int LastNumber);

public record DashboardSummaryDto(
    decimal TotalSalesToday,
    int NumberOfSalesToday,
    decimal TotalPurchasesToday,
    int LowStockItemsCount,
    int ActiveCustomersCount,
    int ActiveSuppliersCount,
    int TotalProductsCount,
    decimal TotalReceivables,
    decimal TotalPayables,
    decimal TotalSalesMonth,
    decimal TotalPurchasesMonth);

public record SalesReportDto(
    DateTime InvoiceDate,
    string InvoiceNo,
    string CustomerName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);

public record PurchaseReportDto(
    DateTime InvoiceDate,
    string InvoiceNo,
    string SupplierName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);

public record StockReportDto(
    int ProductId,
    string ProductName,
    string CategoryName,
    string UnitName,
    string WarehouseName,
    decimal CurrentStock,
    decimal ReorderLevel,
    decimal PurchasePrice,
    decimal TotalValue
);

public record CustomerBalanceReportDto(
    int CustomerId,
    string CustomerName,
    decimal OpeningBalance,
    decimal TotalSales,
    decimal TotalReturns,
    decimal TotalPayments,
    decimal TotalCredit,
    decimal CurrentBalance
);

public record SupplierBalanceReportDto(
    int SupplierId,
    string SupplierName,
    decimal OpeningBalance,
    decimal TotalPurchases,
    decimal TotalReturns,
    decimal TotalPayments,
    decimal TotalDebit,
    decimal CurrentBalance
);

public record ProductMovementReportDto(
    DateTime Date,
    string WarehouseName,
    string MovementType,
    string ReferenceNo,
    decimal QuantityChange,
    decimal QuantityAfter
);

public record LowStockReportDto(
    int     ProductId,
    string  ProductName,
    string? CategoryName,
    string  WarehouseName,
    decimal CurrentRetailQty,
    decimal ReorderLevelRetailQty,
    decimal DeficitRetailQty,
    decimal SuggestedWholesaleBoxes,   // Product.ConvertRetailToWholesaleBoxes()
    decimal SuggestedRetailRemainder,  // Product.GetRemainingRetailAfterWholesale()
    string  WholesaleUnitName,
    string  RetailUnitName,
    decimal ConversionFactor
);




