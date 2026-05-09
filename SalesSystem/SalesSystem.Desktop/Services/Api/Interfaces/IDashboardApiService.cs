using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IDashboardApiService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
}

