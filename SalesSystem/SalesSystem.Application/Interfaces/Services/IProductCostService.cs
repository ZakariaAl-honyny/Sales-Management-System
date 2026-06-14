using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for deriving product cost from InventoryBatches.
/// All costing methods (WeightedAverage, FIFO, LatestCost) are computed
/// from actual batch data rather than a denormalized Product.Cost column.
/// </summary>
public interface IProductCostService
{
    /// <summary>
    /// Computes the weighted average cost from all non-zero-quantity batches
    /// for the given product.
    /// Formula: SUM(QuantityRemaining * UnitCost) / SUM(QuantityRemaining)
    /// Returns 0 if no batches exist or all batches are empty.
    /// </summary>
    Task<Result<decimal>> GetAverageCostAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns the FIFO layers consumed to cover the requested quantity.
    /// Batches are consumed in order of CreatedAt ASC (oldest first).
    /// If total remaining quantity across all batches is less than the
    /// requested quantity, returns what is available.
    /// </summary>
    Task<Result<List<FifoLayerDto>>> GetFifoLayersAsync(int productId, decimal quantity, CancellationToken ct = default);

    /// <summary>
    /// Returns the unit cost of the most recent batch (by CreatedAt DESC).
    /// This represents the latest purchase cost for the product.
    /// Returns 0 if no batches exist.
    /// </summary>
    Task<Result<decimal>> GetLatestCostAsync(int productId, CancellationToken ct = default);
}
