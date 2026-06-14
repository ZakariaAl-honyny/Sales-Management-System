namespace SalesSystem.Contracts.Responses;

/// <summary>
/// DTO for a payment voucher (سند صرف).
/// </summary>
public record PaymentVoucherDto(
    int Id,
    int VoucherNo,
    DateTime VoucherDate,
    short CurrencyId,
    string? CurrencyName,
    string? CurrencyCode,
    int CashBoxId,
    string? CashBoxName,
    int AccountId,
    string? AccountName,
    decimal TotalAmount,
    string? Notes,
    byte Status,
    int? SourceDocumentId,
    string? SourceDocumentType,
    DateTime CreatedAt,
    DateTime? PostedAt,
    DateTime? CancelledAt
);
