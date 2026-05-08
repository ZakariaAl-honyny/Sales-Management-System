using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISalesReturnApiService
{
    Task<Result<IReadOnlyList<SalesReturnDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken ct = default);
}
