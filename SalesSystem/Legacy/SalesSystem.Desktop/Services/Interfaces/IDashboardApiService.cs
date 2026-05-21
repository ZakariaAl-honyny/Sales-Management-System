using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IDashboardApiService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
}

