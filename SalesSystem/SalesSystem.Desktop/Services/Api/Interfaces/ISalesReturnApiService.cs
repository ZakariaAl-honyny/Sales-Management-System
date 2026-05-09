using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ISalesReturnApiService
{
    Task<Result<IReadOnlyList<SalesReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int? customerId = null, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest r, CancellationToken ct = default); Task<Result> PostAsync(int id, CancellationToken ct = default); Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
