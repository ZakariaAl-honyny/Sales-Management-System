namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a batch/lot of inventory for FIFO/FEFO cost allocation.
/// Schema: nvarchar(50) BatchNo, date ExpiryDate (nullable),
/// decimal(18,3) QuantityReceived, decimal(18,3) QuantityRemaining, decimal(18,2) UnitCost,
/// int? PurchaseInvoiceId, int? PurchaseInvoiceLineId.
/// AuditableEntity — hard-deleted, no IsActive.
/// </summary>
public record InventoryBatchDto(
    int Id,
    int ProductId,
    string? ProductName,
    int? PurchaseInvoiceId,
    int? PurchaseInvoiceLineId,
    short WarehouseId,
    string? WarehouseName,
    string BatchNo,
    decimal QuantityReceived,
    decimal QuantityRemaining,
    decimal UnitCost,
    DateOnly? ExpiryDate,
    string? SupplierBatchNo)
{
    public decimal TotalValue => QuantityRemaining * UnitCost;
    public bool IsFullyConsumed => QuantityRemaining <= 0.0001m;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
    public string ExpiryStatus => IsExpired ? "منتهي" : "ساري";
}
