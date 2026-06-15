namespace SalesSystem.Domain.Accounting.Enums;

/// <summary>
/// Status values for ReceiptVouchers and PaymentVouchers.
/// </summary>
public enum VoucherStatus : byte
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}
