using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IUnitOfWork uow, ILogger<EmployeeService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<EmployeeDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var employees = await _uow.Employees.ToListAsync(ct, "Party", "Department");
            var dtos = employees.Select(MapToDto).ToList();
            return Result<List<EmployeeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all employees");
            return Result<List<EmployeeDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الموظفين");
        }
    }

    public async Task<Result<EmployeeDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var employee = await _uow.Employees.FirstOrDefaultAsync(e => e.Id == id, ct, "Party", "Department");
            if (employee == null)
                return Result<EmployeeDto>.Failure("الموظف غير موجود", ErrorCodes.NotFound);

            return Result<EmployeeDto>.Success(MapToDto(employee));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {Id}", id);
            return Result<EmployeeDto>.Failure("حدث خطأ أثناء استرجاع بيانات الموظف");
        }
    }

    public async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct)
    {
        try
        {
            // Validate Party exists
            var partyExists = await _uow.Parties.AnyAsync(p => p.Id == request.PartyId, ct);
            if (!partyExists)
                return Result<EmployeeDto>.Failure("الطرف المحدد غير موجود", ErrorCodes.NotFound);

            // Validate Department exists (if provided)
            if (request.DepartmentId.HasValue)
            {
                var deptExists = await _uow.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
                if (!deptExists)
                    return Result<EmployeeDto>.Failure("القسم المحدد غير موجود", ErrorCodes.NotFound);
            }

            var employee = Employee.Create(
                request.PartyId,
                request.EmployeeNo,
                request.HireDate,
                request.DepartmentId,
                request.Salary,
                request.Notes);

            await _uow.Employees.AddAsync(employee, ct);
            await _uow.SaveChangesAsync(ct);

            // Reload with includes for the DTO
            employee = await _uow.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id, ct, "Party", "Department");

            _logger.LogInformation(
                "Employee created: #{EmployeeNo} (ID: {Id}, PartyId: {PartyId})",
                employee!.EmployeeNo, employee.Id, request.PartyId);

            return Result<EmployeeDto>.Success(MapToDto(employee));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating employee: {Message}", ex.Message);
            return Result<EmployeeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return Result<EmployeeDto>.Failure("حدث خطأ أثناء إنشاء الموظف");
        }
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        try
        {
            var employee = await _uow.Employees.FirstOrDefaultAsync(e => e.Id == id, ct, "Party", "Department");
            if (employee == null)
                return Result<EmployeeDto>.Failure("الموظف غير موجود", ErrorCodes.NotFound);

            // Validate Department exists (if provided)
            if (request.DepartmentId.HasValue)
            {
                var deptExists = await _uow.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
                if (!deptExists)
                    return Result<EmployeeDto>.Failure("القسم المحدد غير موجود", ErrorCodes.NotFound);
            }

            employee.Update(request.DepartmentId, request.Salary, request.Notes);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Employee updated: #{EmployeeNo} (ID: {Id})", employee.EmployeeNo, id);

            return Result<EmployeeDto>.Success(MapToDto(employee));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating employee {Id}: {Message}", id, ex.Message);
            return Result<EmployeeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {Id}", id);
            return Result<EmployeeDto>.Failure("حدث خطأ أثناء تحديث بيانات الموظف");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var employee = await _uow.Employees.GetByIdAsync(id, ct);
            if (employee == null)
                return Result.Failure("الموظف غير موجود", ErrorCodes.NotFound);

            employee.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Employee deactivated: #{EmployeeNo} (ID: {Id})", employee.EmployeeNo, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الموظف");
        }
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto(
            employee.Id,
            employee.PartyId,
            employee.Party?.Name,
            employee.EmployeeNo,
            employee.HireDate,
            employee.DepartmentId,
            employee.Department?.Name,
            employee.Salary,
            employee.Notes,
            employee.IsActive
        );
    }
}
