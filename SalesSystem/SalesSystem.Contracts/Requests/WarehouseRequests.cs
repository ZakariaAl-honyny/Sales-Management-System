namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new warehouse.
/// AccountId is auto-created by the service (not user-supplied).
/// </summary>
public record CreateWarehouseRequest(
    short BranchId,
    string Name,
    string? Phone = null,
    string? Address = null,
    string? Notes = null);

/// <summary>
/// Request to update an existing warehouse.
/// </summary>
public record UpdateWarehouseRequest(
    short BranchId,
    string Name,
    string? Phone = null,
    string? Address = null,
    string? Notes = null,
    bool IsActive = true);

/// <summary>
/// Request to create a new inventory transaction.
/// </summary>
public record CreateInventoryTransactionRequest(
    int TransactionNo,
    byte TransactionType,
    short WarehouseId,
    DateTime? TransactionDate = null,
    byte? ReferenceType = null,
    int? ReferenceId = null,
    string? Notes = null,
    List<CreateInventoryTransactionLineRequest>? Lines = null);

/// <summary>
/// A single line in an inventory transaction.
/// </summary>
public record CreateInventoryTransactionLineRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    int? BatchId = null);

/// <summary>
/// Request to create a new warehouse transfer.
/// </summary>
public record CreateWarehouseTransferRequest(
    int TransferNo,
    short FromWarehouseId,
    short ToWarehouseId,
    DateTime? TransferDate = null,
    string? Notes = null,
    List<CreateWarehouseTransferLineRequest>? Lines = null);

/// <summary>
/// A single line in a warehouse transfer.
/// </summary>
public record CreateWarehouseTransferLineRequest(
    int ProductId,
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    int? BatchId = null);
