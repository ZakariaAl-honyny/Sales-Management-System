using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateCustomerPaymentRequest(
    int CustomerId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    int? SalesInvoiceId = null,
    string? Notes = null
);

public record CreateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    int? PurchaseInvoiceId = null,
    string? Notes = null
);
