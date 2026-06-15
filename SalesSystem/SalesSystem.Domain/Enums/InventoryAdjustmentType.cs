namespace SalesSystem.Domain.Enums;

/// <summary>
/// أنواع تسوية المخزون حسب schema: 1=Opening, 2=Increase, 3=Shortage, 4=Damage.
/// </summary>
public enum InventoryAdjustmentType : byte
{
    Opening = 1,
    Increase = 2,
    Shortage = 3,
    Damage = 4
}
