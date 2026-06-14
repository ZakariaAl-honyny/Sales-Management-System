namespace SalesSystem.Domain.Enums;

/// <summary>
/// Defines the operational type of a warehouse.
/// </summary>
public enum WarehouseType : byte
{
    /// <summary>Main / primary warehouse.</summary>
    Main = 1,

    /// <summary>Store / secondary storage location.</summary>
    Store = 2,

    /// <summary>Showroom — display area, limited stock.</summary>
    Showroom = 3
}
