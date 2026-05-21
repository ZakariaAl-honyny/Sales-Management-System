using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IStockTransferApiService
{
    Task<Result<IReadOnlyList<StockTransferDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<StockTransferDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest r, CancellationToken ct = default);
    Task<Result> PostAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
