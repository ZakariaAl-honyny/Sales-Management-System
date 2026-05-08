using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IPurchaseReturnApiService
{
    Task<Result<IReadOnlyList<PurchaseReturnDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, CancellationToken ct = default);
}
