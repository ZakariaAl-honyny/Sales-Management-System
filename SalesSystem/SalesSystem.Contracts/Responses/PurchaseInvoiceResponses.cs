using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

public record PurchaseInvoiceResponse(
    int Id, string InvoiceNumber,
    int SupplierId, string SupplierName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status, PaymentType PaymentType,
    decimal SubTotal, decimal InvoiceDiscount,
    decimal TaxRate, decimal TaxAmount,
    bool IsTaxInclusive,
    decimal TotalAmount, decimal PaidAmount, decimal DueAmount,
    DateTime InvoiceDate, string? Notes,
    List<PurchaseInvoiceItemResponse> Items
);

public record PurchaseInvoiceItemResponse(
    int Id, int ProductId, string ProductName, string ProductCode,
    decimal Quantity, decimal UnitCost, decimal DiscountAmount, decimal LineTotal
);
