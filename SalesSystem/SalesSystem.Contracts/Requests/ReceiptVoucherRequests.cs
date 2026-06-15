namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a receipt voucher (سند قبض).
/// </summary>
public record CreateReceiptVoucherRequest(
    DateTime VoucherDate,
    short CurrencyId,
    int CashBoxId,
    int AccountId,
    decimal TotalAmount,
    string? Notes = null
);

/// <summary>
/// Request to update a receipt voucher.
/// </summary>
public record UpdateReceiptVoucherRequest(
    DateTime? VoucherDate = null,
    string? Notes = null
);

/// <summary>
/// Request to update receipt voucher total amount.
/// </summary>
public record UpdateVoucherTotalRequest(
    decimal TotalAmount
);
