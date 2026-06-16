using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using System.Linq.Expressions;

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

    public async Task<Result<SystemAccountMappingDto>> GetMappingAsync(SystemAccountKey key, short? branchId = null, CancellationToken ct = default)
    {
        try
        {
            Expression<Func<SystemAccountMapping, bool>> predicate;

            if (branchId.HasValue)
                predicate = m => m.MappingKey == key.ToString() && m.BranchId == branchId.Value;
            else
                predicate = m => m.MappingKey == key.ToString() && m.BranchId == null;

            var mapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(predicate, ct, "Account");

            if (mapping == null)
                return Result<SystemAccountMappingDto>.Failure($"لم يتم تعيين حساب لمفتاح '{key}'");

            return Result<SystemAccountMappingDto>.Success(MapToDto(mapping));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mapping for key {Key}", key);
            return Result<SystemAccountMappingDto>.Failure("حدث خطأ أثناء استرجاع حساب النظام");
        }
    }

    public async Task<Result<List<SystemAccountMappingDto>>> GetAllMappingsAsync(short? branchId = null, CancellationToken ct = default)
    {
        try
        {
            Expression<Func<SystemAccountMapping, bool>> predicate;
            if (branchId.HasValue)
                predicate = m => m.BranchId == branchId.Value;
            else
                predicate = m => true;

            var mappings = await _uow.SystemAccountMappings.ToListAsync(predicate, ct: ct, includePaths: "Account");

            var dtos = mappings.Select(MapToDto).ToList();
            return Result<List<SystemAccountMappingDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all mappings for branch {BranchId}", branchId);
            return Result<List<SystemAccountMappingDto>>.Failure("حدث خطأ أثناء استرجاع حسابات النظام");
        }
    }

    public async Task<Result<SystemAccountMappingDto>> CreateMappingAsync(CreateSystemAccountMappingRequest request, CancellationToken ct = default)
    {
        try
        {
            // Check for duplicate key + branch combination
            var existing = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                m => m.MappingKey == request.MappingKey.ToString() && m.BranchId == request.BranchId, ct: ct);

            if (existing != null)
                return Result<SystemAccountMappingDto>.Failure("يوجد تعيين نشط لنفس المفتاح والفرع");

            var mapping = SystemAccountMapping.Create(
                request.MappingKey.ToString(),
                request.AccountId,
                request.BranchId);

            await _uow.SystemAccountMappings.AddAsync(mapping, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Created system account mapping: Key={Key}, AccountId={AccountId}, BranchId={BranchId}",
                mapping.MappingKey, mapping.AccountId, mapping.BranchId);

            return Result<SystemAccountMappingDto>.Success(MapToDto(mapping));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system account mapping");
            return Result<SystemAccountMappingDto>.Failure("حدث خطأ أثناء إنشاء حساب النظام");
        }
    }

    public async Task<Result<SystemAccountMappingDto>> UpdateMappingAsync(int id, UpdateSystemAccountMappingRequest request, CancellationToken ct = default)
    {
        try
        {
            var mapping = await _uow.SystemAccountMappings.GetByIdAsync(id, ct);
            if (mapping == null)
                return Result<SystemAccountMappingDto>.Failure("حساب النظام غير موجود", ErrorCodes.NotFound);

            mapping.Update(request.AccountId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Updated system account mapping {Id}: AccountId={AccountId}", id, request.AccountId);

            return Result<SystemAccountMappingDto>.Success(MapToDto(mapping));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system account mapping {Id}", id);
            return Result<SystemAccountMappingDto>.Failure("حدث خطأ أثناء تحديث حساب النظام");
        }
    }

    public async Task<Result> DeleteMappingAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var mapping = await _uow.SystemAccountMappings.GetByIdAsync(id, ct);
            if (mapping == null)
                return Result.Failure("حساب النظام غير موجود", ErrorCodes.NotFound);

            _uow.SystemAccountMappings.DeleteRange(new[] { mapping });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted system account mapping {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting system account mapping {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف حساب النظام");
        }
    }

    private static SystemAccountMappingDto MapToDto(SystemAccountMapping mapping)
    {
        return new SystemAccountMappingDto(
            Id: mapping.Id,
            MappingKey: Enum.TryParse<SystemAccountKey>(mapping.MappingKey, out var key)
                ? key
                : SystemAccountKey.DefaultCash,
            MappingKeyName: mapping.MappingKey,
            AccountId: mapping.AccountId,
            AccountName: mapping.Account?.NameAr,
            AccountCode: mapping.Account?.AccountCode,
            BranchId: mapping.BranchId);
    }
}
