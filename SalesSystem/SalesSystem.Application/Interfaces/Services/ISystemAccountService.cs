using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing system account mappings (key-value pattern).
/// Maps business functions to Chart of Accounts AccountIds.
/// </summary>
public interface ISystemAccountService
{
    Task<Result<SystemAccountMappingDto>> GetMappingAsync(SalesSystem.Domain.Accounting.Enums.SystemAccountKey key, short? branchId = null, CancellationToken ct = default);
    Task<Result<List<SystemAccountMappingDto>>> GetAllMappingsAsync(short? branchId = null, CancellationToken ct = default);
    Task<Result<SystemAccountMappingDto>> CreateMappingAsync(SalesSystem.Contracts.Requests.CreateSystemAccountMappingRequest request, CancellationToken ct = default);
    Task<Result<SystemAccountMappingDto>> UpdateMappingAsync(int id, SalesSystem.Contracts.Requests.UpdateSystemAccountMappingRequest request, CancellationToken ct = default);
    Task<Result> DeleteMappingAsync(int id, CancellationToken ct = default);
}
