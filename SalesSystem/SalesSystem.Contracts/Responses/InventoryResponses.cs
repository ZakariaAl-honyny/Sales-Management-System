namespace SalesSystem.Contracts.Responses;

public record InventoryMovementResponse(
    int Id,
    DateTime MovementDate,
    string ProductName,
    string WarehouseName,
    byte MovementType,
    decimal QuantityChange,
    decimal QuantityAfter,
    string ReferenceType,
    string ReferenceId
);
