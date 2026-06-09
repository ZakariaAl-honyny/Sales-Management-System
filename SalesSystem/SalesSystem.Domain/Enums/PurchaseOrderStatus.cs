namespace SalesSystem.Domain.Enums;

/// <summary>
/// حالة أمر الشراء
/// </summary>
public enum PurchaseOrderStatus : byte
{
    /// <summary>مسودة — لم يتم اعتماده بعد</summary>
    Draft = 1,

    /// <summary>معتمد — جاهز للاستلام</summary>
    Approved = 2,

    /// <summary>مستلم جزئياً — تم استلام جزء من الكمية</summary>
    PartiallyReceived = 3,

    /// <summary>مستلم بالكامل — تم تحويل جميع الأصناف لفواتير شراء</summary>
    Received = 4,

    /// <summary>ملغي</summary>
    Cancelled = 5
}
