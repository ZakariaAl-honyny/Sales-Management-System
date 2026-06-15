namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a batch/lot of inventory for FIFO/FEFO cost allocation.
/// Phase 25: Uses single Quantity field (no separate Received/Remaining tracking).
/// SupplierBatchNo removed — batch tracking via PurchaseInvoiceId.
/// </summary>
public record InventoryBatchDto(
    int Id,
    int ProductId,
    string? ProductName,
    int? PurchaseInvoiceId,
    int WarehouseId,
    string? WarehouseName,
    decimal Quantity,
    decimal UnitCost,
    string? BatchNo,
    DateTime? ExpiryDate,
    bool IsActive)
{
    public decimal TotalValue => Quantity * UnitCost;

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;

    public string ExpiryStatus => IsExpired ? "منتهي" : "ساري";

    public bool IsLowStock => Quantity <= 0;
}
