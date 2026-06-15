namespace SalesSystem.Contracts.Enums;

/// <summary>
/// Payment method used for customer/supplier payments.
/// Distinct from PaymentType (which is invoice-level Cash/Credit/Mixed).
/// </summary>
public enum PaymentMethod : byte
{
    Cash = 1,
    Cheque = 2,
    BankTransfer = 3,
    CreditCard = 4
}
