namespace SalesSystem.Domain.Enums;

/// <summary>
/// طريقة توزيع الرسوم الإضافية على أصناف الفاتورة
/// </summary>
public enum DistributionMethod : byte
{
    /// <summary>توزيع حسب تكلفة الصنف (نسبة من إجمالي الفاتورة)</summary>
    ByCost = 0,

    /// <summary>توزيع حسب كمية الصنف</summary>
    ByQuantity = 1
}
