namespace SalesSystem.Domain.Enums;

public enum PurchaseOrderStatus : byte
{
    Draft = 1,
    Approved = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}
