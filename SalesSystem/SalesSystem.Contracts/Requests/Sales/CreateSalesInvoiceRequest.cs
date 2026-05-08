using SalesSystem.Domain.Enums;

namespace SalesSystem.Contracts.Requests.Sales;

public record CreateSalesInvoiceRequest(
    int WarehouseId,
    int? CustomerId,
    DateTime? InvoiceDate,
    DateOnly? DueDate,
    PaymentType PaymentType,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal PaidAmount,
    string? Notes,
    List<CreateSalesInvoiceItemRequest> Items);

public record CreateSalesInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    string? Notes);
