namespace SalesSystem.Contracts.Responses;

public record SupplierPaymentApplicationDto(
    int Id,
    int SupplierPaymentId,
    int PurchaseInvoiceId,
    int? InvoiceNo,
    decimal AppliedAmount,
    bool IsActive
);
