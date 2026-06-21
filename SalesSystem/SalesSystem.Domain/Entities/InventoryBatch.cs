using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory received via a single purchase.
/// Enables FIFO/FEFO cost allocation.
/// Maps to "InventoryBatches" table.
/// Schema: nvarchar(50) BatchNo, date ExpiryDate (nullable),
/// decimal(18,3) QuantityReceived, decimal(18,3) QuantityRemaining, decimal(18,2) UnitCost,
/// int? PurchaseInvoiceLineId FK (nullable).
/// AuditableEntity — hard-deleted, no IsActive.
/// </summary>
public class InventoryBatch : AuditableEntity
{
    /// <summary>
    /// Batch/lot number (nvarchar(50)).
    /// </summary>
    public string BatchNo { get; private set; } = string.Empty;

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
    /// FK to the PurchaseInvoiceLine that brought in this stock.
    /// </summary>
    public int? PurchaseInvoiceLineId { get; private set; }

    /// <summary>
    /// Supplier's batch/lot reference number. nvarchar(100).
    /// </summary>
    public string? SupplierBatchNo { get; private set; }

    /// <summary>
    /// Expiry date (if applicable). Used for FEFO picking.
    /// </summary>
    public DateOnly? ExpiryDate { get; private set; }

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

    /// <summary>
    /// Indicates whether this batch is closed for further mutations.
    /// Set to true when QuantityRemaining reaches zero (fully consumed).
    /// Once closed, no further Deduct, IncreaseRemaining, UpdateExpiry, or UpdateBatchNo
    /// operations are allowed.
    /// </summary>
    public bool IsClosed { get; private set; }

    // Navigation properties
    public virtual Product? Product { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual PurchaseInvoiceLine? PurchaseInvoiceLine { get; private set; }

    /// <summary>
    /// Guards against mutations on a closed batch (fully consumed).
    /// Throws <see cref="DomainException"/> if the batch is already closed.
    /// </summary>
    private void IsClosedGuard()
    {
        if (QuantityRemaining <= 0 || IsClosed)
            throw new DomainException("لا يمكن تعديل الدفعة بعد استهلاكها بالكامل");
    }

    private InventoryBatch() { } // EF Core

    /// <summary>
    /// Creates a new inventory batch record.
    /// </summary>
    public static InventoryBatch Create(
        string batchNo,
        int productId,
        short warehouseId,
        decimal quantityReceived,
        decimal unitCost,
        int? purchaseInvoiceId = null,
        int? purchaseInvoiceLineId = null,
        string? supplierBatchNo = null,
        DateOnly? expiryDate = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(batchNo))
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
            BatchNo = batchNo.Trim(),
            ProductId = productId,
            WarehouseId = warehouseId,
            PurchaseInvoiceId = purchaseInvoiceId,
            PurchaseInvoiceLineId = purchaseInvoiceLineId,
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
        IsClosedGuard();
        if (quantity <= 0)
            throw new DomainException("كمية السحب يجب أن تكون أكبر من صفر.");
        if (quantity > QuantityRemaining + 0.0001m)
            throw new DomainException(
                $"الكمية المطلوبة ({quantity:N3}) تتجاوز المتاح في الدفعة ({QuantityRemaining:N3}).");

        QuantityRemaining -= quantity;
        if (IsFullyConsumed)
            IsClosed = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Increases the remaining quantity. Used for returns or adjustments.
    /// </summary>
    public void IncreaseRemaining(decimal quantity)
    {
        IsClosedGuard();
        if (quantity <= 0)
            throw new DomainException("كمية الإضافة يجب أن تكون أكبر من صفر.");
        QuantityRemaining += quantity;
        // Reopening the batch — clear IsClosed flag
        if (IsClosed && QuantityRemaining > 0)
            IsClosed = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the expiry date (for corrections).
    /// </summary>
    public void UpdateExpiry(DateOnly? newExpiryDate)
    {
        IsClosedGuard();
        ExpiryDate = newExpiryDate;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the batch number.
    /// </summary>
    public void UpdateBatchNo(string newBatchNo)
    {
        IsClosedGuard();
        if (string.IsNullOrWhiteSpace(newBatchNo))
            throw new DomainException("رقم الدفعة مطلوب.");
        BatchNo = newBatchNo.Trim();
        UpdateTimestamp();
    }
}
