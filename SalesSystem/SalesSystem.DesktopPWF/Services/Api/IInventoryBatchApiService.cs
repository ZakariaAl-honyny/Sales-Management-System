using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IInventoryBatchApiService
{
    Task<Result<List<InventoryBatchDto>>> GetByProductAsync(int productId, int? warehouseId, CancellationToken ct = default);
    Task<Result<InventoryBatchDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<InventoryBatchDto>> CreateAsync(CreateInventoryBatchRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
}
