using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing inventory write-offs (الإتلاف)
/// </summary>
public interface IInventoryWriteOffService
{
    /// <summary>
    /// Writes off expired/damaged stock from inventory.
    /// Opens a transaction, creates StockWriteOff record, decreases stock, and records InventoryMovement.
    /// </summary>
    Task<Result<StockWriteOffDto>> WriteOffExpiredStockAsync(CreateStockWriteOffRequest request, int userId, CancellationToken ct);
}
