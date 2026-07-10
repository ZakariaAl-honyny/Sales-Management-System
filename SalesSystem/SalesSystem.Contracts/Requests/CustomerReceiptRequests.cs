namespace SalesSystem.Contracts.Requests;

public record CreateCustomerReceiptRequest(
    int CustomerId,
    int CashBoxId,
    decimal Amount,
    string? Notes = null,
    byte PaymentMethod = 1
);

public record UpdateCustomerReceiptRequest(
    int CashBoxId,
    decimal Amount,
    string? Notes = null,
    byte PaymentMethod = 1
);

public record AddReceiptApplicationRequest(
    int CustomerReceiptId,
    int SalesInvoiceId,
    decimal AppliedAmount
);
