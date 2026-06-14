using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime? PaymentDate,
    int? PurchaseInvoiceId,
    int? CashBoxId,
    string? Notes = null
);
