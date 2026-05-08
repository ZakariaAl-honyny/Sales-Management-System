namespace SalesSystem.Contracts.Enums;

public enum UserRole : byte { Admin = 1, Manager = 2, Cashier = 3 }

public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }

public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }

public enum MovementType : byte
{
    PurchaseIn = 1, 
    SaleOut = 2, 
    SaleReturnIn = 3,
    PurchaseReturnOut = 4, 
    TransferOut = 5, 
    TransferIn = 6, 
    Adjustment = 7
}
