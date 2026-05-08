namespace SalesSystem.Domain.Enums;

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
