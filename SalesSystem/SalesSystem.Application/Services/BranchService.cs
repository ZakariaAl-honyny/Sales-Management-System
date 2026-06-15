using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BranchService> _logger;

    public BranchService(IUnitOfWork uow, ILogger<BranchService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<BranchDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var branches = await _uow.Branches.ToListAsync(ct);
            var dtos = branches.Select(MapToDto).ToList();
            return Result<List<BranchDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all branches");
            return Result<List<BranchDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الفروع");
        }
    }

    public async Task<Result<BranchDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var branch = await _uow.Branches.GetByIdAsync(id, ct);
            if (branch == null)
                return Result<BranchDto>.Failure("الفرع غير موجود", ErrorCodes.NotFound);

            return Result<BranchDto>.Success(MapToDto(branch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving branch {Id}", id);
            return Result<BranchDto>.Failure("حدث خطأ أثناء استرجاع بيانات الفرع");
        }
    }

    public async Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request, CancellationToken ct)
    {
        try
        {
            var branch = Branch.Create(request.Name);

            await _uow.Branches.AddAsync(branch, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Branch created: {Name} (ID: {Id})", branch.Name, branch.Id);

            return Result<BranchDto>.Success(MapToDto(branch));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating branch: {Message}", ex.Message);
            return Result<BranchDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return Result<BranchDto>.Failure("حدث خطأ أثناء إنشاء الفرع");
        }
    }

    public async Task<Result<BranchDto>> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct)
    {
        try
        {
            var branch = await _uow.Branches.GetByIdAsync(id, ct);
            if (branch == null)
                return Result<BranchDto>.Failure("الفرع غير موجود", ErrorCodes.NotFound);

            branch.Update(request.Name);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Branch updated: {Name} (ID: {Id})", branch.Name, id);

            return Result<BranchDto>.Success(MapToDto(branch));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating branch {Id}: {Message}", id, ex.Message);
            return Result<BranchDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branch {Id}", id);
            return Result<BranchDto>.Failure("حدث خطأ أثناء تحديث بيانات الفرع");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var branch = await _uow.Branches.GetByIdAsync(id, ct);
            if (branch == null)
                return Result.Failure("الفرع غير موجود", ErrorCodes.NotFound);

            branch.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Branch deactivated: {Name} (ID: {Id})", branch.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating branch {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الفرع");
        }
    }

    private static BranchDto MapToDto(Branch branch)
    {
        return new BranchDto(
            branch.Id,
            branch.Name,
            branch.IsActive
        );
    }
}
