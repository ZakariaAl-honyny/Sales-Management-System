namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new inventory batch.
/// Schema: nvarchar(50) BatchNo, date ExpiryDate (nullable),
/// decimal(18,3) QuantityReceived, decimal(18,3) QuantityRemaining (set to QuantityReceived),
/// decimal(18,2) UnitCost, int? PurchaseInvoiceId, int? PurchaseInvoiceLineId.
/// </summary>
public record CreateInventoryBatchRequest(
    int ProductId,
    short WarehouseId,
    decimal QuantityReceived,
    decimal UnitCost,
    string BatchNo,
    int? PurchaseInvoiceId = null,
    int? PurchaseInvoiceLineId = null,
    string? SupplierBatchNo = null,
    DateOnly? ExpiryDate = null);
