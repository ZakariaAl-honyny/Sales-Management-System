namespace SalesSystem.Domain.Enums;

/// <summary>
/// Transaction types for inventory transactions.
/// Maps to InventoryTransactionType enum in database — tinyint.
/// </summary>
public enum InventoryTransactionType : byte
{
    Purchase = 1,
    PurchaseReturn = 2,
    Sale = 3,
    SaleReturn = 4,
    TransferOut = 5,
    TransferIn = 6,
    Count = 7,
    Adjustment = 8,
    Damage = 9,
    OpeningBalance = 10,
    InternalIssue = 11,
    InternalReceipt = 12
}
