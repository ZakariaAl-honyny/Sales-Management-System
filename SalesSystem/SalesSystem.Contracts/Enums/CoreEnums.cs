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

public enum SaleMode : byte { Retail = 1, Wholesale = 2 }

/// <summary>
/// حالة أمر الشراء: مسودة، معتمد، مستلم جزئياً، مستلم بالكامل، ملغي
/// </summary>
public enum PurchaseOrderStatus : byte { Draft = 1, Approved = 2, PartiallyReceived = 3, Received = 4, Cancelled = 5 }

/// <summary>
/// نوع الخصم: مبلغ أو نسبة مئوية
/// </summary>
public enum DiscountType : byte { Amount = 0, Percentage = 1 }

/// <summary>
/// طريقة توزيع المصاريف الإضافية: حسب التكلفة أو حسب الكمية
/// </summary>
public enum DistributionMethod : byte { ByCost = 0, ByQuantity = 1 }
