namespace SalesSystem.Domain.Enums;

/// <summary>
/// Defines how product cost is calculated when new stock is purchased.
/// </summary>
public enum CostingMethod : byte
{
    /// <summary>
    /// متوسط التكلفة المرجح — Weighted Average Cost
    /// Formula: [(OldQty × OldCost) + (NewQty × NewCost)] / TotalQty
    /// </summary>
    WeightedAverage = 0,

    /// <summary>
    /// آخر سعر توريد — Last Purchase Price
    /// Simply uses the price from the most recent purchase invoice.
    /// </summary>
    LastPurchasePrice = 1,

    /// <summary>
    /// سعر المورد — Supplier Catalog Price
    /// Uses the supplier's catalog price (not the actual invoice price).
    /// </summary>
    SupplierPrice = 2
}