using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

public record SalesInvoiceResponse(
    int Id, string InvoiceNumber,
    int CustomerId, string CustomerName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status, PaymentType PaymentType,
    decimal SubTotal, decimal InvoiceDiscount,
    decimal TaxRate, decimal TaxAmount,
    bool IsTaxInclusive,
    decimal TotalAmount, decimal PaidAmount, decimal DueAmount,
    DateTime InvoiceDate, string? Notes,
    List<SalesInvoiceLineResponse> Items
);

public record SalesInvoiceLineResponse(
    int Id, int ProductId, string ProductName,
    decimal Quantity, decimal UnitPrice, decimal DiscountAmount, decimal LineTotal
);
