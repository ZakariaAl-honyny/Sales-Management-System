using SalesSystem.Domain.Common;

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
}