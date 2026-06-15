namespace SalesSystem.Contracts.Responses;

/// <summary>
/// DTO for a receipt voucher (سند قبض).
/// </summary>
public record ReceiptVoucherDto(
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
    DateTime CreatedAt,
    DateTime? PostedAt,
    DateTime? CancelledAt
);
