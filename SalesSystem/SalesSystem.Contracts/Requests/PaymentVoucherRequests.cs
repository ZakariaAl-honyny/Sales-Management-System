namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a payment voucher (سند صرف).
/// </summary>
public record CreatePaymentVoucherRequest(
    DateTime VoucherDate,
    short CurrencyId,
    int CashBoxId,
    int AccountId,
    decimal TotalAmount,
    string? Notes = null,
    int? SourceDocumentId = null,
    string? SourceDocumentType = null
);

/// <summary>
/// Request to update a payment voucher.
/// </summary>
public record UpdatePaymentVoucherRequest(
    DateTime? VoucherDate = null,
    string? Notes = null,
    int? SourceDocumentId = null,
    string? SourceDocumentType = null
);
