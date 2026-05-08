using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Inventory;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IStockTransferApiService
{
    Task<Result<IReadOnlyList<StockTransferDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<StockTransferDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest request, CancellationToken ct = default);
}
