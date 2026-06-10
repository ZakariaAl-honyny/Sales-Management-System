using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class ProductPriceHistory : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public decimal OldValue { get; private set; }
    public decimal NewValue { get; private set; }
    public string? CostingMethod { get; private set; }
    public int? InvoiceId { get; private set; }
    public int ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }

    // ─── Detailed Price History Fields (Phase 2) ────────────────────────

    public decimal OldRetailPrice { get; private set; }
    public decimal NewRetailPrice { get; private set; }
    public decimal OldWholesalePrice { get; private set; }
    public decimal NewWholesalePrice { get; private set; }
    public decimal OldCost { get; private set; }
    public decimal NewCost { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;
    public int ChangedByUserId { get; private set; }

    // Navigation
    public ProductUnit ProductUnit { get; private set; } = null!;

    private ProductPriceHistory() { }

    public static ProductPriceHistory Create(
        int productUnitId,
        string changeType,
        decimal oldValue,
        decimal newValue,
        string? costingMethod = null,
        int? invoiceId = null,
        int changedBy = 0)
    {
        return new ProductPriceHistory
        {
            ProductUnitId = productUnitId,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            CostingMethod = costingMethod,
            InvoiceId = invoiceId,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a detailed price history record. Immutable after creation.
    /// Use this for Phase 2 cost cascade and manual price adjustments.
    /// </summary>
    public static ProductPriceHistory CreateWithDetails(
        int productUnitId,
        decimal oldRetailPrice,
        decimal newRetailPrice,
        decimal oldWholesalePrice,
        decimal newWholesalePrice,
        decimal oldCost,
        decimal newCost,
        string changeReason,
        int changedByUserId)
    {
        if (string.IsNullOrWhiteSpace(changeReason))
            throw new DomainException("سبب التغيير مطلوب");

        return new ProductPriceHistory
        {
            ProductUnitId = productUnitId,
            OldRetailPrice = oldRetailPrice,
            NewRetailPrice = newRetailPrice,
            OldWholesalePrice = oldWholesalePrice,
            NewWholesalePrice = newWholesalePrice,
            OldCost = oldCost,
            NewCost = newCost,
            ChangeReason = changeReason,
            ChangedByUserId = changedByUserId,
            ChangeType = "DetailedUpdate",
            OldValue = oldCost,
            NewValue = newCost,
            ChangedBy = changedByUserId,
            ChangedAt = DateTime.UtcNow
        };
    }
}