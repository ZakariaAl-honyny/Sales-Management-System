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
    WeightedAverage = 1,

    /// <summary>
    /// آخر سعر توريد — Last Purchase Price
    /// Simply uses the price from the most recent purchase invoice.
    /// </summary>
    LastPurchasePrice = 2,

    /// <summary>
    /// سعر المورد — Supplier Catalog Price
    /// Uses the supplier's catalog price (not the actual invoice price).
    /// </summary>
    SupplierPrice = 3,

    /// <summary>
    /// الوارد أولاً صادر أولاً — First In, First Out (FIFO)
    /// Cost is assigned based on the oldest purchase batch first.
    /// Requires batch-level tracking (InventoryBatches) for accurate costing.
    /// </summary>
    FIFO = 4
}