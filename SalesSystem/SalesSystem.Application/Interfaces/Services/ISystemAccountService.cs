using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISystemAccountService
{
    Task<Result<SystemAccountMappings>> GetMappingsAsync(int? branchId = null, CancellationToken ct = default);
}
