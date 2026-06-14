namespace SalesSystem.Contracts.Requests;

public record CreateCustomerReceiptRequest(
    int CustomerId,
    int CashBoxId,
    int CurrencyId,
    decimal Amount,
    string? Notes = null
);

public record UpdateCustomerReceiptRequest(
    int CashBoxId,
    int CurrencyId,
    decimal Amount,
    string? Notes = null
);

public record AddReceiptApplicationRequest(
    int CustomerReceiptId,
    int SalesInvoiceId,
    decimal AppliedAmount
);
