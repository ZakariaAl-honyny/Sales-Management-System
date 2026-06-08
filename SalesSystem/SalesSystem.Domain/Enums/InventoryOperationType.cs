namespace SalesSystem.Domain.Enums;

public enum InventoryOperationType : byte
{
    StockIssue = 1,
    StockReceipt = 2,
    Adjustment = 3
}
