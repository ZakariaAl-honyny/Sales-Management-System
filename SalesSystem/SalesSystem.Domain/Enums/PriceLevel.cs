namespace SalesSystem.Domain.Enums;

/// <summary>
/// Defines the pricing tiers available for products.
/// Each level can have a different price per currency per product unit.
/// </summary>
public enum PriceLevel : byte
{
    /// <summary>
    /// سعر التجزئة — Standard retail price for individual consumers
    /// </summary>
    Retail = 1,

    /// <summary>
    /// سعر الجملة — Wholesale price for bulk buyers
    /// </summary>
    Wholesale = 2,

    /// <summary>
    /// سعر VIP — Special pricing for VIP customers
    /// </summary>
    VIP = 3,

    /// <summary>
    /// سعر الموزع — Distributor pricing for channel partners
    /// </summary>
    Distributor = 4
}
