using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks stock quantity per product per warehouse.
/// Inherits <see cref="AuditableEntity"/> for audit trail.
/// Maps to "WarehouseStocks" table.
/// </summary>
public class WarehouseStock : AuditableEntity
{
    public short WarehouseId { get; private set; }
    public int ProductId { get; private set; }

    /// <summary>
    /// Current stock quantity in base units. decimal(18,3).
    /// DB CHECK constraint ensures Quantity >= 0.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Weighted average cost per base unit. decimal(18,2).
    /// Updated on each purchase/receipt via costing method.
    /// </summary>
    public decimal AvgCost { get; private set; }

    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Product? Product { get; private set; }

    private WarehouseStock() { }

    public static WarehouseStock Create(
        short warehouseId,
        int productId,
        decimal quantity = 0,
        decimal avgCost = 0,
        int? createdByUserId = null)
    {
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");
        if (avgCost < 0)
            throw new DomainException("متوسط التكلفة لا يمكن أن يكون سالباً.");

        var stock = new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AvgCost = avgCost
        };
        stock.SetCreatedBy(createdByUserId);
        return stock;
    }

    public void IncreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        Quantity += amount;
        UpdateTimestamp();
    }

    public void DecreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        if (Quantity < amount)
            throw new DomainException("المخزون غير كافٍ.");
        Quantity -= amount;
        UpdateTimestamp();
    }

    public void SetQuantity(decimal quantity)
    {
        if (quantity < 0)
            throw new DomainException("الكمية لا يمكن أن تكون سالبة.");
        Quantity = quantity;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the weighted average cost.
    /// Formula: (OldQuantity * OldAvgCost + NewQuantity * NewUnitCost) / (OldQuantity + NewQuantity)
    /// When OldQuantity = 0, AvgCost = NewUnitCost.
    /// </summary>
    public void UpdateAvgCost(decimal newQuantity, decimal newUnitCost)
    {
        if (newQuantity < 0)
            throw new DomainException("الكمية الجديدة لا يمكن أن تكون سالبة.");
        if (newUnitCost < 0)
            throw new DomainException("تكلفة الوحدة الجديدة لا يمكن أن تكون سالبة.");

        if (Quantity + newQuantity == 0)
        {
            AvgCost = 0;
        }
        else
        {
            var totalOldValue = Quantity * AvgCost;
            var totalNewValue = newQuantity * newUnitCost;
            AvgCost = Math.Round((totalOldValue + totalNewValue) / (Quantity + newQuantity), 2);
        }

        // Also update the quantity to reflect the new stock level
        Quantity += newQuantity;
        UpdateTimestamp();
    }

    public void DeductStock(decimal quantity, SalesSystem.Domain.Enums.UnitType unitType, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        var quantityInPieces = unitType == SalesSystem.Domain.Enums.UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        if (Quantity < quantityInPieces)
            throw new DomainException($"المخزون غير كافٍ. المتاح: {Quantity}, المطلوب: {quantityInPieces}");

        Quantity -= quantityInPieces;
        UpdateTimestamp();
    }

    public void AddStock(decimal quantity, SalesSystem.Domain.Enums.UnitType unitType, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        var quantityInPieces = unitType == SalesSystem.Domain.Enums.UnitType.Wholesale
            ? quantity * conversionFactor
            : quantity;

        Quantity += quantityInPieces;
        UpdateTimestamp();
    }
}
