using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory received via a single purchase.
/// Enables FIFO/FEFO cost allocation per product unit.
/// Each purchase invoice item creates one InventoryBatch (or more if the item has different lot numbers).
/// Maps to "InventoryBatches" table with FK to PurchaseInvoice.
/// </summary>
public class InventoryBatch : ActivatableEntity
{
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
    /// FK to PurchaseInvoiceItem — identifies which line item this batch came from.
    /// </summary>
    public int? PurchaseInvoiceItemId { get; private set; }

    /// <summary>
    /// Current remaining quantity in stock for this batch. Base units.
    /// decimal(18,3) in DB.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Unit cost per base unit for this batch. decimal(18,2) in DB.
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Batch/Lot number from the supplier. Max 50 chars.
    /// </summary>
    public string? BatchNo { get; private set; }

    /// <summary>
    /// Date of manufacture (if applicable).
    /// </summary>
    public DateTime? ManufactureDate { get; private set; }

    /// <summary>
    /// Expiry date (if applicable). Used for FEFO picking.
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Indicates whether this batch is active (not fully consumed or deleted).
    /// </summary>
    public new bool IsActive { get; private set; } = true;

    /// <summary>
    /// Indicates whether this batch is fully consumed (Quantity = 0).
    /// </summary>
    public bool IsFullyConsumed => Math.Abs(Quantity) < 0.0001m;

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual Warehouse Warehouse { get; private set; } = null!;
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }

    private InventoryBatch() { } // EF Core

    /// <summary>
    /// Creates a new inventory batch record.
    /// </summary>
    public static InventoryBatch Create(
        int productId,
        short warehouseId,
        decimal quantity,
        decimal unitCost,
        int? purchaseInvoiceId = null,
        int? purchaseInvoiceItemId = null,
        string? batchNo = null,
        DateTime? manufactureDate = null,
        DateTime? expiryDate = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("معرف المنتج مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("معرف المستودع مطلوب.");
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        var batch = new InventoryBatch
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = Math.Round(quantity, 3),
            UnitCost = Math.Round(unitCost, 2),
            PurchaseInvoiceId = purchaseInvoiceId,
            PurchaseInvoiceItemId = purchaseInvoiceItemId,
            BatchNo = batchNo?.Trim(),
            ManufactureDate = manufactureDate,
            ExpiryDate = expiryDate,
            IsActive = true
        };
        batch.SetCreatedBy(createdByUserId);
        return batch;
    }

    /// <summary>
    /// Reduces the quantity in this batch. Used when selling or transferring stock.
    /// </summary>
    public void DecreaseQuantity(decimal quantityToRemove)
    {
        if (quantityToRemove <= 0)
            throw new DomainException("كمية السحب يجب أن تكون أكبر من صفر.");

        if (Quantity - quantityToRemove < -0.0001m)
            throw new DomainException(
                $"الكمية المطلوبة ({quantityToRemove:N3}) تتجاوز المتاح في الدفعة ({Quantity:N3}).");

        Quantity = Math.Round(Quantity - quantityToRemove, 3);
        UpdateTimestamp();
    }

    /// <summary>
    /// Increases the quantity in this batch. Used for returns or adjustments.
    /// </summary>
    public void IncreaseQuantity(decimal quantityToAdd)
    {
        if (quantityToAdd <= 0)
            throw new DomainException("كمية الإضافة يجب أن تكون أكبر من صفر.");

        Quantity = Math.Round(Quantity + quantityToAdd, 3);
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
    /// Updates the batch/lot number.
    /// </summary>
    public void UpdateBatchNo(string? newBatchNo)
    {
        BatchNo = newBatchNo?.Trim();
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates the batch (soft delete).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
