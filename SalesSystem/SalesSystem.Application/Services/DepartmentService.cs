using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(IUnitOfWork uow, ILogger<DepartmentService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var departments = await _uow.Departments.ToListAsync(ct);
            var dtos = departments.Select(MapToDto).ToList();
            return Result<List<DepartmentDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all departments");
            return Result<List<DepartmentDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الأقسام");
        }
    }

    public async Task<Result<DepartmentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var department = await _uow.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (department == null)
                return Result<DepartmentDto>.Failure("القسم غير موجود", ErrorCodes.NotFound);

            return Result<DepartmentDto>.Success(MapToDto(department));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {Id}", id);
            return Result<DepartmentDto>.Failure("حدث خطأ أثناء استرجاع بيانات القسم");
        }
    }

    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct)
    {
        try
        {
            var department = Department.Create(request.Name);

            await _uow.Departments.AddAsync(department, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Department created: {Name} (ID: {Id})",
                department.Name, department.Id);

            return Result<DepartmentDto>.Success(MapToDto(department));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating department: {Message}", ex.Message);
            return Result<DepartmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return Result<DepartmentDto>.Failure("حدث خطأ أثناء إنشاء القسم");
        }
    }

    public async Task<Result<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken ct)
    {
        try
        {
            var department = await _uow.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (department == null)
                return Result<DepartmentDto>.Failure("القسم غير موجود", ErrorCodes.NotFound);

            department.Update(request.Name);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Department updated: {Name} (ID: {Id})", department.Name, id);

            return Result<DepartmentDto>.Success(MapToDto(department));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating department {Id}: {Message}", id, ex.Message);
            return Result<DepartmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {Id}", id);
            return Result<DepartmentDto>.Failure("حدث خطأ أثناء تحديث بيانات القسم");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var department = await _uow.Departments.GetByIdAsync(id, ct);
            if (department == null)
                return Result.Failure("القسم غير موجود", ErrorCodes.NotFound);

            department.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Department deactivated: {Name} (ID: {Id})", department.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating department {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط القسم");
        }
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto(
            department.Id,
            department.Name,
            department.IsActive
        );
    }
}
