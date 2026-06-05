using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Application.Accounting.Services;

public class SystemAccountService : ISystemAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SystemAccountService> _logger;

    public SystemAccountService(IUnitOfWork uow, ILogger<SystemAccountService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<SystemAccountMappings>> GetMappingsAsync(int? branchId = null, CancellationToken ct = default)
    {
        try
        {
            // Try branch-specific first, fall back to global (branchId == null)
            var mappings = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                m => (branchId == null && m.BranchId == null) || (branchId != null && m.BranchId == branchId),
                ct: ct);

            if (mappings == null)
                return Result<SystemAccountMappings>.Failure("لم يتم إعداد حسابات النظام — يرجى الاتصال بالمسؤول");

            return Result<SystemAccountMappings>.Success(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system account mappings for branch {BranchId}", branchId);
            return Result<SystemAccountMappings>.Failure("حدث خطأ أثناء استرجاع حسابات النظام");
        }
    }
}
