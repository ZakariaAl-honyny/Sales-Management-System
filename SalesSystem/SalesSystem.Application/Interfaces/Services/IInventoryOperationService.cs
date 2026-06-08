using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IInventoryOperationService
{
    Task<Result<InventoryOperationDto>> GetByIdAsync(int id, CancellationToken ct);

    Task<Result<PagedResult<InventoryOperationDto>>> GetAllAsync(
        int? warehouseId, byte? operationType, int page, int pageSize, CancellationToken ct);

    Task<Result<InventoryOperationDto>> CreateAsync(
        CreateInventoryOperationRequest request, int userId, CancellationToken ct);

    Task<Result<InventoryOperationDto>> PostAsync(int id, int userId, CancellationToken ct);

    Task<Result<InventoryOperationDto>> CancelAsync(int id, int userId, CancellationToken ct);
}
