namespace SalesSystem.Domain.Enums;

/// <summary>
/// Types of cash transactions for audit trail.
/// </summary>
public enum CashTransactionType : byte
{
    /// <summary>
    /// مبيعات (وارد) — Cash from sales collected into box
    /// </summary>
    SaleIn = 0,

    /// <summary>
    /// مشتريات (صادر) — Cash paid out for purchases
    /// </summary>
    PurchaseOut = 1,

    /// <summary>
    /// تحويل وارد — Cash transferred IN from another box
    /// </summary>
    TransferIn = 2,

    /// <summary>
    /// تحويل صادر — Cash transferred OUT to another box
    /// </summary>
    TransferOut = 3,

    /// <summary>
    /// إيداع يدوي — Manual deposit (e.g., customer pays cash)
    /// </summary>
    ManualIn = 4,

    /// <summary>
    /// سحب يدوي — Manual withdrawal (e.g., owner takes cash)
    /// </summary>
    ManualOut = 5
}