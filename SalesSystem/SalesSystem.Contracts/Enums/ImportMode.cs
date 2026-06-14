namespace SalesSystem.Contracts.Enums;

/// <summary>
/// وضع استيراد المنتجات: إدراج جديد أو تحديث البيانات الموجودة
/// </summary>
public enum ImportMode : byte
{
    /// <summary>إدراج منتجات جديدة (الوضع الافتراضي)</summary>
    Insert = 1,

    /// <summary>تحديث المنتجات الموجودة حسب الباركود</summary>
    Update = 2
}
