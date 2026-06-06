using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISystemAccountService
{
    Task<Result<SystemAccountMappingsDto>> GetMappingsAsync(int? branchId = null, CancellationToken ct = default);
}
