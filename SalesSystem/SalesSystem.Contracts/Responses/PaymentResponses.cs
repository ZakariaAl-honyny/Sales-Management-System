namespace SalesSystem.Contracts.Responses;

public record CustomerPaymentResponse(
    int Id, string PaymentNumber,
    int CustomerId, string CustomerName,
    decimal Amount, int? InvoiceId,
    DateTime PaymentDate, string? Notes
);

public record SupplierPaymentResponse(
    int Id, string PaymentNumber,
    int SupplierId, string SupplierName,
    decimal Amount, int? InvoiceId,
    DateTime PaymentDate, string? Notes
);
