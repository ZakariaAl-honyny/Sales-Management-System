using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class WarehouseStock
{
    public int WarehouseStockId { get; private set; }
    public int WarehouseId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Product? Product { get; private set; }

    private WarehouseStock() { }

    public static WarehouseStock Create(int warehouseId, int productId, decimal quantity = 0)
    {
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));

        return new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void IncreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        Quantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecreaseQuantity(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (Quantity < amount)
            throw new InvalidOperationException("Insufficient stock.");
        Quantity -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetQuantity(decimal quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}