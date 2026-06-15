namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Response for Warehouse entity — no AccountId, IsDefault, Code, Type, Location, or ManagerName per schema.
/// </summary>
public record WarehouseResponse(
    int Id,
    string Name,
    string? Phone,
    string? Address,
    string? Notes,
    bool IsActive);

/// <summary>
/// Simplified stock summary response.
/// </summary>
public record WarehouseStockSummaryResponse(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal AvgCost);

/// <summary>
/// Response for InventoryTransaction entity.
/// </summary>
public record InventoryTransactionResponse(
    int Id,
    int TransactionNo,
    DateTime TransactionDate,
    byte TransactionType,
    short WarehouseId,
    string? WarehouseName,
    int? ReferenceId,
    byte? ReferenceType,
    string? Notes,
    byte Status,
    List<InventoryTransactionLineResponse>? Lines);

/// <summary>
/// A single line within an InventoryTransaction.
/// </summary>
public record InventoryTransactionLineResponse(
    int Id,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    int? BatchId);

/// <summary>
/// Response for WarehouseTransfer entity.
/// </summary>
public record WarehouseTransferResponse(
    int Id,
    int TransferNo,
    short SourceWarehouseId,
    string? SourceWarehouseName,
    short DestinationWarehouseId,
    string? DestinationWarehouseName,
    DateTime TransferDate,
    string? Notes,
    byte Status,
    List<WarehouseTransferLineResponse>? Lines);

/// <summary>
/// A single line within a WarehouseTransfer.
/// </summary>
public record WarehouseTransferLineResponse(
    int Id,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    int? BatchId);
