using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IInventoryCountService
{
    Task<Result<List<InventoryCountDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<InventoryCountDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<InventoryCountDto>> CreateAsync(CreateInventoryCountRequest request, int userId, CancellationToken ct);
    Task<Result<InventoryCountDto>> AddLineAsync(int countId, AddInventoryCountLineRequest request, CancellationToken ct);
    Task<Result> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result> CancelAsync(int id, CancellationToken ct);
}
