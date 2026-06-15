namespace SalesSystem.Contracts.Responses;

public record CustomerReceiptDto(
    int Id,
    int ReceiptNo,
    DateTime ReceiptDate,
    int CustomerId,
    string? CustomerName,
    int CashBoxId,
    string? CashBoxName,
    int CurrencyId,
    string? CurrencyName,
    decimal Amount,
    string? Notes,
    byte Status,
    string? StatusName,
    DateTime? PostedAt,
    List<CustomerReceiptApplicationDto>? Applications,
    bool IsActive
);

public record CustomerReceiptApplicationDto(
    int Id,
    int CustomerReceiptId,
    int SalesInvoiceId,
    int? InvoiceNo,
    decimal AppliedAmount,
    bool IsActive
);
