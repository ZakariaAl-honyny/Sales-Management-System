namespace SalesSystem.Domain.Enums;

/// <summary>
/// Reference types for inventory transactions — identifies the source document.
/// Maps to ReferenceType (tinyint) in InventoryTransactions table.
/// </summary>
public enum InventoryReferenceType : byte
{
    PurchaseInvoice = 1,
    SalesInvoice = 2,
    PurchaseReturn = 3,
    SalesReturn = 4,
    Transfer = 5,
    Count = 6,
    Adjustment = 7
}
