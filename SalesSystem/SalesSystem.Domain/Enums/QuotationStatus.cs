namespace SalesSystem.Domain.Enums;

/// <summary>
/// حالة عرض السعر (أمر البيع)
/// </summary>
public enum QuotationStatus : byte
{
    /// <summary>مسودة — قابلة للتعديل</summary>
    Draft = 1,

    /// <summary>مؤكد — تم تأكيدها للعميل</summary>
    Confirmed = 2,

    /// <summary>منتهية الصلاحية — لم يتم تحويلها لفاتورة</summary>
    Expired = 3,

    /// <summary>تم التحويل — تم تحويلها لفاتورة بيع</summary>
    Converted = 4
}
