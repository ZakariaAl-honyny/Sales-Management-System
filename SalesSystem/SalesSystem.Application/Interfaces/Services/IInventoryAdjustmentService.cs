using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IInventoryAdjustmentService
{
    Task<Result<List<InventoryAdjustmentDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<InventoryAdjustmentDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<InventoryAdjustmentDto>> CreateAsync(CreateInventoryAdjustmentRequest request, int userId, CancellationToken ct);
    Task<Result<InventoryAdjustmentDto>> AddLineAsync(int adjustmentId, AddInventoryAdjustmentLineRequest request, CancellationToken ct);
    Task<Result> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result> CancelAsync(int id, CancellationToken ct);
}
