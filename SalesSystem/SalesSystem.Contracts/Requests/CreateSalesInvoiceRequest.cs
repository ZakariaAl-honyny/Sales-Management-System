using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateSalesInvoiceRequest(
    int WarehouseId,
    int? InvoiceNo,
    int CustomerId,
    int? CashBoxId,
    DateTime? InvoiceDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal PaidAmount,
    string? Notes,
    short? CurrencyId,
    decimal? ExchangeRate,
    int? TaxId,
    List<CreateSalesInvoiceLineRequest> Items);

public record CreateSalesInvoiceLineRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    int ProductUnitId);
