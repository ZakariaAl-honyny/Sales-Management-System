namespace SalesSystem.Domain.Accounting.Enums;

public enum JournalEntryType : byte
{
    Sales = 1,
    SalesReturn = 2,
    Purchase = 3,
    PurchaseReturn = 4,
    Expense = 5,
    StockWriteOff = 6,
    Transfer = 7,
    Manual = 8,
    OpeningBalance = 9,
    CustomerReceipt = 10,
    SupplierPayment = 11
}
