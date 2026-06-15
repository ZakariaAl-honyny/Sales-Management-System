namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// Represents a FIFO inventory layer consumed for cost allocation.
/// Each layer corresponds to a portion of an InventoryBatch.
/// </summary>
public record FifoLayerDto(
    int BatchId,
    string? BatchNo,
    decimal QuantityConsumed,
    decimal UnitCost,
    decimal TotalCost  // computed: QuantityConsumed * UnitCost
);
