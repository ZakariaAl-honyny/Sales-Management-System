using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IPurchaseReturnApiService
{
    Task<Result<IReadOnlyList<PurchaseReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int? supplierId = null, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest r, CancellationToken ct = default); Task<Result> PostAsync(int id, CancellationToken ct = default); Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
