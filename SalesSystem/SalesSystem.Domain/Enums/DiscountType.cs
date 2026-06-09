namespace SalesSystem.Domain.Enums;

/// <summary>
/// نوع الخصم — مبلغ ثابت أو نسبة مئوية
/// </summary>
public enum DiscountType : byte
{
    /// <summary>خصم بمبلغ ثابت</summary>
    Amount = 0,

    /// <summary>خصم بنسبة مئوية</summary>
    Percentage = 1
}
