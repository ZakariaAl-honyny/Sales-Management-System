namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a batch/lot of inventory for FIFO/FEFO cost allocation.
/// </summary>
public record InventoryBatchDto(
    int Id,
    int ProductId,
    string? ProductName,
    int? PurchaseInvoiceItemId,
    int WarehouseId,
    string? WarehouseName,
    decimal Quantity,
    decimal UnitCost,
    string BatchNo,
    DateTime? ManufactureDate,
    DateTime? ExpiryDate,
    bool IsActive)
{
    public decimal TotalValue => Quantity * UnitCost;

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;

    public string ExpiryStatus => IsExpired ? "منتهي" : "ساري";

    public bool IsLowStock => Quantity <= 0;
}
