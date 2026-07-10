using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// استجابة فاتورة الشراء.
/// </summary>
public record PurchaseInvoiceResponse(
    int Id, string InvoiceNumber,
    int SupplierId, string SupplierName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status, PaymentType PaymentType,
    decimal SubTotal, decimal InvoiceDiscount,
    decimal TaxRate, decimal TaxAmount,
    bool IsTaxInclusive,
    decimal NetTotal, decimal PaidAmount, decimal RemainingAmount,
    DateTime InvoiceDate,
    string? AttachmentPath,
    string? Notes,
    List<PurchaseInvoiceLineResponse> Items
);

/// <summary>
/// استجابة بند فاتورة الشراء.
/// </summary>
public record PurchaseInvoiceLineResponse(
    int Id, int ProductId, string ProductName,
    int ProductUnitId, string ProductUnitName,
    decimal Quantity, decimal UnitCost, decimal LineTotal
);
