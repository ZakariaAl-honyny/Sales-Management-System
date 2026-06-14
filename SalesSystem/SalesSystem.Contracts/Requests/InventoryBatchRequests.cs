namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new inventory batch.
/// WarehouseId is smallint (short).
/// </summary>
public record CreateInventoryBatchRequest(
    int ProductId,
    short WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    string BatchNo,
    int? PurchaseInvoiceId = null,
    DateTime? ExpiryDate = null,
    DateTime? ManufactureDate = null);
