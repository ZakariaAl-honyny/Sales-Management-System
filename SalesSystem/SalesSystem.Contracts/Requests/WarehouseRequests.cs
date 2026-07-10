namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new warehouse.
/// AccountId is auto-created by the service (not user-supplied).
/// </summary>
public record CreateWarehouseRequest(
    string Name,
    string? Phone = null,
    string? Address = null,
    string? Notes = null);

/// <summary>
/// Request to update an existing warehouse.
/// </summary>
public record UpdateWarehouseRequest(
    string Name,
    string? Phone = null,
    string? Address = null,
    string? Notes = null,
    bool IsActive = true);

/// <summary>
/// Request to create a new inventory transaction.
/// </summary>
public record CreateInventoryTransactionRequest(
    string? TransactionNo,
    byte MovementType,
    short WarehouseId,
    byte? ReferenceType = null,
    int? ReferenceId = null,
    string? Notes = null,
    List<CreateInventoryTransactionLineRequest>? Lines = null);

/// <summary>
/// A single line in an inventory transaction.
/// </summary>
public record CreateInventoryTransactionLineRequest(
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    string? BatchNo = null,
    DateOnly? ExpiryDate = null,
    short? WarehouseId = null);

/// <summary>
/// Request to create a new warehouse transfer.
/// </summary>
public record CreateWarehouseTransferRequest(
    string? TransferNo,
    short SourceWarehouseId,
    short DestinationWarehouseId,
    string? Notes = null,
    List<CreateWarehouseTransferLineRequest>? Lines = null);

/// <summary>
/// A single line in a warehouse transfer.
/// </summary>
public record CreateWarehouseTransferLineRequest(
    int ProductUnitId,
    decimal Quantity,
    string? BatchNo = null);
