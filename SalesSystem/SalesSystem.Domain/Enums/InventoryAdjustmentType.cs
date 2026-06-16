namespace SalesSystem.Domain.Enums;

/// <summary>
/// أنواع تسوية المخزون حسب schema: 1=Addition, 2=Deduction, 3=Correction.
/// </summary>
public enum InventoryAdjustmentType : byte
{
    Addition = 1,
    Deduction = 2,
    Correction = 3
}
