using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a batch/lot of inventory for FIFO/FEFO cost allocation.
/// Each batch tracks quantity received and quantity remaining for a specific product in a specific warehouse.
/// </summary>
public class InventoryBatch : BaseEntity
{
    public int ProductId { get; private set; }
    public int? PurchaseInvoiceItemId { get; private set; }
    public int WarehouseId { get; private set; }
    /// <summary>
    /// Current available quantity in base units.
    /// For purchase receipts, this represents the remaining stock after deductions.
    /// The "received vs remaining" semantics are handled at the business-logic level
    /// (e.g., RecordReceipt increases Quantity, DeductStock decreases it).
    /// </summary>
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public DateTime? ManufactureDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string BatchNo { get; private set; } = string.Empty;

    public Product? Product { get; private set; }
    public PurchaseInvoiceItem? PurchaseInvoiceItem { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private InventoryBatch() { }

    public static InventoryBatch Create(
        int productId,
        int warehouseId,
        decimal quantity,
        decimal unitCost,
        string batchNo,
        int? purchaseInvoiceItemId = null,
        DateTime? manufactureDate = null,
        DateTime? expiryDate = null,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("معرف المنتج مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("معرف المستودع مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (string.IsNullOrWhiteSpace(batchNo))
            throw new DomainException("رقم الدفعة/الباتش مطلوب.");
        if (manufactureDate.HasValue && expiryDate.HasValue && manufactureDate >= expiryDate)
            throw new DomainException("تاريخ التصنيع يجب أن يكون قبل تاريخ انتهاء الصلاحية.");

        var batch = new InventoryBatch
        {
            ProductId = productId,
            PurchaseInvoiceItemId = purchaseInvoiceItemId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            UnitCost = Math.Round(unitCost, 2),
            ManufactureDate = manufactureDate,
            ExpiryDate = expiryDate,
            BatchNo = batchNo.Trim(),
            IsActive = true
        };
        batch.SetCreatedBy(createdByUserId);
        return batch;
    }

    public void DeductStock(decimal qty)
    {
        if (qty <= 0)
            throw new DomainException("الكمية المستهلكة يجب أن تكون أكبر من الصفر.");
        if (qty > Quantity)
            throw new DomainException(
                $"الكمية المطلوبة ({qty}) تتجاوز الكمية المتوفرة في الدفعة ({Quantity}).");
        Quantity -= qty;
        UpdateTimestamp();
    }

    public InventoryBatch TransferStock(decimal qty)
    {
        if (qty <= 0)
            throw new DomainException("الكمية المحولة يجب أن تكون أكبر من الصفر.");
        if (qty > Quantity)
            throw new DomainException(
                $"الكمية المطلوب تحويلها ({qty}) تتجاوز الكمية المتوفرة ({Quantity}).");
        Quantity -= qty;
        var newBatch = new InventoryBatch
        {
            ProductId = ProductId,
            PurchaseInvoiceItemId = PurchaseInvoiceItemId,
            WarehouseId = WarehouseId,
            Quantity = qty,
            UnitCost = UnitCost,
            ManufactureDate = ManufactureDate,
            ExpiryDate = ExpiryDate,
            BatchNo = BatchNo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = CreatedByUserId
        };
        UpdateTimestamp();
        return newBatch;
    }

    public bool IsExpiredOn(DateTime date)
        => ExpiryDate.HasValue && date >= ExpiryDate.Value;

    public decimal TotalValue => Quantity * UnitCost;
}
