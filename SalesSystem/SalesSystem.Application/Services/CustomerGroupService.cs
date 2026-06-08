using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CustomerGroupService : ICustomerGroupService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerGroupService> _logger;

    public CustomerGroupService(IUnitOfWork uow, ILogger<CustomerGroupService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CustomerGroupDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var group = await _uow.CustomerGroups.GetByIdAsync(id, ct);
        if (group == null)
            return Result<CustomerGroupDto>.Failure("مجموعة العملاء غير موجودة", ErrorCodes.NotFound);

        return Result<CustomerGroupDto>.Success(MapToDto(group));
    }

    public async Task<Result<List<CustomerGroupDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var groups = await _uow.CustomerGroups.ToListAsync(ct);
            var dtos = groups.Select(MapToDto).ToList();
            return Result<List<CustomerGroupDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching customer groups");
            return Result<List<CustomerGroupDto>>.Failure("حدث خطأ أثناء تحميل مجموعات العملاء.");
        }
    }

    public async Task<Result<CustomerGroupDto>> CreateAsync(CreateCustomerGroupRequest request, CancellationToken ct)
    {
        try
        {
            var group = CustomerGroup.Create(request.Name, request.Description, createdByUserId: null);

            await _uow.CustomerGroups.AddAsync(group, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer group created: {GroupName} (ID: {GroupId})", group.Name, group.Id);

            return Result<CustomerGroupDto>.Success(MapToDto(group));
        }
        catch (DomainException ex)
        {
            return Result<CustomerGroupDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating customer group");
            return Result<CustomerGroupDto>.Failure("حدث خطأ أثناء إضافة مجموعة العملاء.");
        }
    }

    public async Task<Result<CustomerGroupDto>> UpdateAsync(int id, UpdateCustomerGroupRequest request, CancellationToken ct)
    {
        try
        {
            var group = await _uow.CustomerGroups.FirstOrDefaultIgnoreFiltersAsync(g => g.Id == id, ct);
            if (group == null)
                return Result<CustomerGroupDto>.Failure("مجموعة العملاء غير موجودة", ErrorCodes.NotFound);

            group.Update(request.Name, request.Description, updatedByUserId: null);

            if (request.IsActive != group.IsActive)
            {
                if (request.IsActive) group.Restore();
                else group.MarkAsDeleted();
            }

            await _uow.CustomerGroups.UpdateAsync(group, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer group updated: {GroupName} (ID: {GroupId})", group.Name, group.Id);

            return Result<CustomerGroupDto>.Success(MapToDto(group));
        }
        catch (DomainException ex)
        {
            return Result<CustomerGroupDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating customer group {Id}", id);
            return Result<CustomerGroupDto>.Failure("حدث خطأ أثناء تحديث مجموعة العملاء.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        // Prevent deletion if any customers are linked to this group
        if (await _uow.Customers.AnyAsync(c => c.CustomerGroupId == id, ct))
            return Result.Failure("لا يمكن حذف المجموعة لأنها مرتبطة بعملاء");

        var group = await _uow.CustomerGroups.GetByIdAsync(id, ct);
        if (group == null)
            return Result.Failure("مجموعة العملاء غير موجودة", ErrorCodes.NotFound);

        group.MarkAsDeleted();
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Customer group soft-deleted: {GroupId}", id);
        return Result.Success();
    }

    private static CustomerGroupDto MapToDto(CustomerGroup g)
    {
        return new CustomerGroupDto(g.Id, g.Name, g.Description, g.IsActive);
    }
}
