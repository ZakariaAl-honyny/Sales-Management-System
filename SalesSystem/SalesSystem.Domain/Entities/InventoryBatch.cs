using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory received via a single purchase.
/// Enables FIFO/FEFO cost allocation.
/// Maps to "InventoryBatches" table.
/// Schema: BatchNo (int), SupplierBatchNo (varchar 100 nullable), ExpiryDate (date null),
/// QuantityReceived (decimal 18,3), QuantityRemaining (decimal 18,3), UnitCost (decimal 18,2).
/// </summary>
public class InventoryBatch : AuditableEntity
{
    /// <summary>
    /// Internal batch number (int).
    /// </summary>
    public int BatchNo { get; private set; }

    /// <summary>
    /// FK to Product.
    /// </summary>
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to Warehouse (smallint in DB).
    /// </summary>
    public short WarehouseId { get; private set; }

    /// <summary>
    /// FK to the PurchaseInvoice that brought in this stock.
    /// </summary>
    public int? PurchaseInvoiceId { get; private set; }

    /// <summary>
    /// Supplier's batch/lot reference number. varchar(100).
    /// </summary>
    public string? SupplierBatchNo { get; private set; }

    /// <summary>
    /// Expiry date (if applicable). Used for FEFO picking.
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Quantity originally received. decimal(18,3).
    /// </summary>
    public decimal QuantityReceived { get; private set; }

    /// <summary>
    /// Quantity remaining in stock. decimal(18,3).
    /// Decreases on sale/transfer.
    /// </summary>
    public decimal QuantityRemaining { get; private set; }

    /// <summary>
    /// Unit cost per base unit for this batch. decimal(18,2).
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Indicates whether this batch is fully consumed (QuantityRemaining = 0).
    /// </summary>
    public bool IsFullyConsumed => QuantityRemaining <= 0.0001m;

    // Navigation properties
    public virtual Product? Product { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }

    private InventoryBatch() { } // EF Core

    /// <summary>
    /// Creates a new inventory batch record.
    /// </summary>
    public static InventoryBatch Create(
        int batchNo,
        int productId,
        short warehouseId,
        decimal quantityReceived,
        decimal unitCost,
        int? purchaseInvoiceId = null,
        string? supplierBatchNo = null,
        DateTime? expiryDate = null,
        int? createdByUserId = null)
    {
        if (batchNo <= 0)
            throw new DomainException("رقم الدفعة مطلوب.");
        if (productId <= 0)
            throw new DomainException("معرف المنتج مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("معرف المستودع مطلوب.");
        if (quantityReceived < 0)
            throw new DomainException("الكمية المستلمة لا يمكن أن تكون سالبة.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        var batch = new InventoryBatch
        {
            BatchNo = batchNo,
            ProductId = productId,
            WarehouseId = warehouseId,
            PurchaseInvoiceId = purchaseInvoiceId,
            SupplierBatchNo = supplierBatchNo?.Trim(),
            ExpiryDate = expiryDate,
            QuantityReceived = quantityReceived,
            QuantityRemaining = quantityReceived,
            UnitCost = unitCost
        };
        batch.SetCreatedBy(createdByUserId);
        return batch;
    }

    /// <summary>
    /// Reduces the remaining quantity in this batch. Used when selling or transferring stock.
    /// </summary>
    public void Deduct(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("كمية السحب يجب أن تكون أكبر من صفر.");
        if (quantity > QuantityRemaining + 0.0001m)
            throw new DomainException(
                $"الكمية المطلوبة ({quantity:N3}) تتجاوز المتاح في الدفعة ({QuantityRemaining:N3}).");

        QuantityRemaining -= quantity;
        UpdateTimestamp();
    }

    /// <summary>
    /// Increases the remaining quantity. Used for returns or adjustments.
    /// </summary>
    public void IncreaseRemaining(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("كمية الإضافة يجب أن تكون أكبر من صفر.");
        QuantityRemaining += quantity;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the expiry date (for corrections).
    /// </summary>
    public void UpdateExpiry(DateTime? newExpiryDate)
    {
        ExpiryDate = newExpiryDate;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the batch number.
    /// </summary>
    public void UpdateBatchNo(string? newBatchNo)
    {
        BatchNo = int.TryParse(newBatchNo, out var parsed) ? parsed : 0;
        UpdateTimestamp();
    }
}
