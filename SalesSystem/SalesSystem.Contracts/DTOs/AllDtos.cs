namespace SalesSystem.Contracts.DTOs;

public record UserDto(int Id, string UserName, string FullName, byte Role);

public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);

public record CategoryDto(int Id, string Name, string? Description, bool IsActive);

public record ProductDto(
    int Id,
    string? Code,
    string? Barcode,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int? UnitId,
    string? UnitName,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description,
    bool IsActive);

public record WarehouseDto(int Id, string? Code, string Name, string? Location, bool IsDefault, bool IsActive);

public record WarehouseStockDto(int Id, int WarehouseId, string WarehouseName, int ProductId, string ProductName, decimal Quantity);

public record SupplierDto(int Id, string? Code, string Name, string? Phone, string? Email, string? Address, decimal OpeningBalance, decimal CurrentBalance, bool IsActive);

public record CustomerDto(int Id, string? Code, string Name, string? Phone, string? Email, string? Address, decimal OpeningBalance, decimal CurrentBalance, bool IsActive);

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
    IReadOnlyList<SalesInvoiceItemDto> Items);

public record SalesInvoiceItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal);

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
    string? Notes,
    byte Status,
    IReadOnlyList<PurchaseInvoiceItemDto> Items);

public record PurchaseInvoiceItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    decimal LineTotal);

public record SalesReturnDto(
    int Id,
    string ReturnNo,
    int? SalesInvoiceId,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime ReturnDate,
    string? Reason,
    decimal SubTotal,
    decimal TotalAmount,
    byte Status,
    IReadOnlyList<SalesReturnItemDto> Items);

public record SalesReturnItemDto(int Id, int ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public record PurchaseReturnDto(
    int Id,
    string ReturnNo,
    int? PurchaseInvoiceId,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    DateTime ReturnDate,
    string? Reason,
    decimal SubTotal,
    decimal TotalAmount,
    byte Status,
    IReadOnlyList<PurchaseReturnItemDto> Items);

public record PurchaseReturnItemDto(int Id, int ProductId, string ProductName, decimal Quantity, decimal UnitCost, decimal LineTotal);

public record StockTransferDto(
    int Id,
    string TransferNo,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    DateTime TransferDate,
    string? Notes,
    byte Status,
    IReadOnlyList<StockTransferItemDto> Items);

public record StockTransferItemDto(int Id, int ProductId, string ProductName, decimal Quantity, string? Notes);

public record CustomerPaymentDto(
    int Id,
    string PaymentNo,
    int CustomerId,
    string CustomerName,
    int? SalesInvoiceId,
    DateTime PaymentDate,
    decimal Amount,
    byte PaymentMethod,
    string? ReferenceNo,
    string? Notes);

public record SupplierPaymentDto(
    int Id,
    string PaymentNo,
    int SupplierId,
    string SupplierName,
    int? PurchaseInvoiceId,
    DateTime PaymentDate,
    decimal Amount,
    byte PaymentMethod,
    string? ReferenceNo,
    string? Notes);

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
    decimal? UnitCost,
    DateTime MovementDate);

public record StoreSettingsDto(
    int Id,
    string StoreName,
    string? Phone,
    string? Address,
    string? LogoPath,
    string CurrencyCode,
    decimal DefaultTaxRate,
    bool IsTaxEnabled);

public record DocumentSequenceDto(int Id, string DocumentType, string Prefix, int Year, int LastNumber);