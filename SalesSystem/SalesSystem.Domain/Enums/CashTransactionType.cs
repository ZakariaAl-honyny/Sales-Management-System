namespace SalesSystem.Domain.Enums;

/// <summary>
/// Types of cash transactions for audit trail.
/// </summary>
public enum CashTransactionType : byte
{
    /// <summary>
    /// الرصيد الافتتاحي — Initial opening balance
    /// </summary>
    OpeningBalance = 1,

    /// <summary>
    /// مبيعات (وارد) — Cash from sales collected into box
    /// </summary>
    SalesIncome = 2,

    /// <summary>
    /// مصروفات (صادر) — Cash paid out as expenses
    /// </summary>
    Expense = 3,

    /// <summary>
    /// تحويل صادر — Cash transferred OUT to another box
    /// </summary>
    TransferOut = 4,

    /// <summary>
    /// تحويل وارد — Cash transferred IN from another box
    /// </summary>
    TransferIn = 5,

    /// <summary>
    /// مرتجع مبيعات (صادر) — Refund paid out for sales return
    /// </summary>
    RefundOut = 6,

    /// <summary>
    /// دفع مورد (صادر) — Payment made to supplier
    /// </summary>
    SupplierPayment = 7,

    /// <summary>
    /// دفع عميل (وارد) — Payment received from customer
    /// </summary>
    CustomerPayment = 8
}