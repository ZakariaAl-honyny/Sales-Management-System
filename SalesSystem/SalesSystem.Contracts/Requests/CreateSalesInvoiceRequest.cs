using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateSalesInvoiceRequest(
    int WarehouseId,
    int? InvoiceNo,
    int? CustomerId,
    int? CashBoxId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    string? Notes,
    int? CurrencyId,
    decimal? ExchangeRate,
    int? TaxId,
    List<CreateSalesInvoiceItemRequest> Items);
public record CreateSalesInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    SaleMode Mode,
    string? Notes,
    int? ProductUnitId = null,
    bool IsPriceOverridden = false);
