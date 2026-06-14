namespace SalesSystem.Domain.Enums;

/// <summary>
/// أنواع تسوية المخزون: إضافة, خصم, تصحيح.
/// </summary>
public enum InventoryAdjustmentType : byte
{
    Addition = 1,
    Deduction = 2,
    Correction = 3
}
