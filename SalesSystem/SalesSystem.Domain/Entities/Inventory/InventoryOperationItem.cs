using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class InventoryOperationItem : BaseEntity
{
    public int InventoryOperationId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public StockIssueReason? StockIssueReason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual InventoryOperation? InventoryOperation { get; private set; }
    public virtual Product? Product { get; private set; }

    private InventoryOperationItem() { }

    public static InventoryOperationItem Create(
        int productId,
        decimal quantity,
        decimal? unitCost = null,
        StockIssueReason? stockIssueReason = null,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");

        return new InventoryOperationItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            StockIssueReason = stockIssueReason,
            Notes = notes
        };
    }
}
