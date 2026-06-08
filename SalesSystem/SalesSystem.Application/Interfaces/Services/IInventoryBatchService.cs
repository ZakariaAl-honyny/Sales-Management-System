using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing inventory batches (FIFO/FEFO tracking).
/// </summary>
public interface IInventoryBatchService
{
    /// <summary>
    /// Gets all active batches for a product, optionally filtered by warehouse.
    /// </summary>
    Task<Result<List<InventoryBatchDto>>> GetByProductAsync(int productId, int? warehouseId, CancellationToken ct);

    /// <summary>
    /// Gets a single inventory batch by ID.
    /// </summary>
    Task<Result<InventoryBatchDto>> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new inventory batch (opening stock or manual entry).
    /// </summary>
    Task<Result<InventoryBatchDto>> CreateAsync(CreateInventoryBatchRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Soft-deletes (deactivates) an inventory batch.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
